/**
 * jobs.js - JavaScript for Email Jobs management
 * This file handles the job creation wizard, template selection, recipient configuration,
 * and job submission functionality
 */

document.addEventListener('DOMContentLoaded', function () {
    initializeJobsPage();
});

/**
 * Initialize the Jobs page
 */
function initializeJobsPage() {
    loadTemplates();
    loadEmailAccounts();
    loadGroups();
    setupEventHandlers();
}

/**
 * Set up all event handlers for the page
 */
function setupEventHandlers() {
    // Modal open/close
    document.getElementById('createJobButton').addEventListener('click', function () {
        const modal = new bootstrap.Modal(document.getElementById('createJobModal'));
        modal.show();
    });

    // Wizard navigation
    document.getElementById('step1NextBtn').addEventListener('click', function () {
        goToStep(2);
    });

    document.getElementById('step2BackBtn').addEventListener('click', function () {
        goToStep(1);
    });

    document.getElementById('step2NextBtn').addEventListener('click', function () {
        updateReviewStep();
        goToStep(3);
    });

    document.getElementById('step3BackBtn').addEventListener('click', function () {
        goToStep(2);
    });

    document.getElementById('submitJobBtn').addEventListener('click', submitJob);

    // Template selection change
    document.getElementById('templateSelect').addEventListener('change', function () {
        if (this.value) {
            fetchTemplateDetails(this.value);
        } else {
            hideTemplatePreview();
        }
    });

    // Recipient type toggle
    document.querySelectorAll('input[name="recipientType"]').forEach(function (radio) {
        radio.addEventListener('change', function () {
            toggleRecipientOptions(this.value);
        });
    });

    // Custom fields management
    document.getElementById('addCustomFieldBtn').addEventListener('click', addCustomField);

    // Event delegation for remove custom field buttons
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-custom-field')) {
            removeCustomField(e.target);
        }
    });

    // Delete job
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('delete-job')) {
            const jobId = e.target.getAttribute('data-job-id');
            showDeleteConfirmation(jobId);
        }
    });

    // Retry job
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('retry-job')) {
            const jobId = e.target.getAttribute('data-job-id');
            retryJob(jobId);
        }
    });

    // Delete confirmation
    document.getElementById('confirmDeleteButton').addEventListener('click', confirmDelete);
}

/**
 * Navigate to a specific step in the wizard
 * @param {number} step - The step number to show
 */
function goToStep(step) {
    // Hide all steps
    document.querySelectorAll('.step-content').forEach(function (el) {
        el.classList.remove('active');
    });

    // Hide all indicators
    document.querySelectorAll('.step').forEach(function (el) {
        el.classList.remove('active');
    });

    // Show the target step
    document.getElementById(`step${step}-content`).classList.add('active');
    document.getElementById(`step${step}-indicator`).classList.add('active');

    // Validate current step
    if (step === 2) {
        validateStep1();
    } else if (step === 3) {
        validateStep2();
    }
}

/**
 * Validate the first step inputs
 */
function validateStep1() {
    const jobName = document.getElementById('jobName').value;
    const templateId = document.getElementById('templateSelect').value;
    const fromEmail = document.getElementById('fromEmailSelect').value;

    if (!jobName || !templateId || !fromEmail) {
        alert('Please fill in all required fields before proceeding.');
        goToStep(1);
        return false;
    }

    return true;
}

/**
 * Validate the second step inputs
 */
function validateStep2() {
    const recipientType = document.querySelector('input[name="recipientType"]:checked').value;

    if (recipientType === 'basic') {
        const recipients = document.getElementById('recipients').value.trim();
        if (!recipients) {
            alert('Please enter at least one recipient.');
            goToStep(2);
            return false;
        }
    } else {
        const groupId = document.getElementById('groupSelect').value;
        if (!groupId) {
            alert('Please select a group.');
            goToStep(2);
            return false;
        }
    }

    return true;
}

/**
 * Toggle between individual recipients and group options
 * @param {string} type - The type of recipient ('basic' or 'group')
 */
function toggleRecipientOptions(type) {
    const basicOptions = document.getElementById('basicRecipientOptions');
    const groupOptions = document.getElementById('groupRecipientOptions');

    if (type === 'basic') {
        basicOptions.classList.remove('d-none');
        groupOptions.classList.add('d-none');
    } else {
        basicOptions.classList.add('d-none');
        groupOptions.classList.remove('d-none');
    }
}

/**
 * Add a new custom field row
 */
function addCustomField() {
    const container = document.getElementById('customFieldsContainer');
    const newRow = document.createElement('div');
    newRow.className = 'row mb-2 custom-field-row';
    newRow.innerHTML = `
        <div class="col-5">
            <input type="text" class="form-control form-control-sm custom-field-key" placeholder="Field name">
        </div>
        <div class="col-6">
            <input type="text" class="form-control form-control-sm custom-field-value" placeholder="Value">
        </div>
        <div class="col-1">
            <button type="button" class="btn btn-sm btn-outline-danger remove-custom-field">×</button>
        </div>
    `;
    container.appendChild(newRow);
}

/**
 * Remove a custom field row
 * @param {HTMLElement} button - The remove button element
 */
function removeCustomField(button) {
    const row = button.closest('.custom-field-row');
    const container = document.getElementById('customFieldsContainer');

    // Keep at least one row
    if (container.querySelectorAll('.custom-field-row').length > 1) {
        container.removeChild(row);
    } else {
        // Just clear inputs if it's the last row
        row.querySelectorAll('input').forEach(input => input.value = '');
    }
}

/**
 * Update the review step with collected data
 */
function updateReviewStep() {
    const jobName = document.getElementById('jobName').value;
    const templateName = document.getElementById('templateSelect').options[document.getElementById('templateSelect').selectedIndex].text;
    const fromEmail = document.getElementById('fromEmailSelect').value;
    const recipientType = document.querySelector('input[name="recipientType"]:checked').value;

    document.getElementById('reviewJobName').textContent = jobName;
    document.getElementById('reviewTemplate').textContent = templateName;
    document.getElementById('reviewFromEmail').textContent = fromEmail;

    // Recipients
    let recipientsText = '';
    if (recipientType === 'basic') {
        const recipients = document.getElementById('recipients').value.trim().split('\n');
        recipientsText = recipients.length + ' individual recipient' + (recipients.length !== 1 ? 's' : '');
    } else {
        const groupSelect = document.getElementById('groupSelect');
        recipientsText = 'Group: ' + groupSelect.options[groupSelect.selectedIndex].text;
    }
    document.getElementById('reviewRecipients').textContent = recipientsText;

    // CC & BCC
    document.getElementById('reviewCC').textContent = document.getElementById('ccInput').value || 'None';
    document.getElementById('reviewBCC').textContent = document.getElementById('bccInput').value || 'None';
}

/**
 * Load available email templates
 */
function loadTemplates() {
    fetch('/api/email-templates')
        .then(response => response.json())
        .then(data => {
            const select = document.getElementById('templateSelect');
            // Clear existing options except the first one
            while (select.options.length > 1) {
                select.remove(1);
            }

            data.forEach(template => {
                const option = document.createElement('option');
                option.value = template.id;
                option.textContent = template.name;
                select.appendChild(option);
            });
        })
        .catch(error => {
            console.error('Error loading templates:', error);
        });
}

/**
 * Load available email accounts
 */
function loadEmailAccounts() {
    fetch('/api/email-accounts')
        .then(response => response.json())
        .then(data => {
            const select = document.getElementById('fromEmailSelect');
            // Clear existing options except the first one
            while (select.options.length > 1) {
                select.remove(1);
            }

            data.forEach(item => {
                const option = document.createElement('option');
                option.value = item.email;        
                option.textContent = item.email;  
                select.appendChild(option);
            });
        })
        .catch(error => {
            console.error('Error loading email accounts:', error);
        });
}

/**
 * Load available recipient groups
 */
function loadGroups() {
    fetch('/api/groups')
        .then(response => response.json())
        .then(data => {
            const select = document.getElementById('groupSelect');
            // Clear existing options except the first one
            while (select.options.length > 1) {
                select.remove(1);
            }

            data.forEach(group => {
                const option = document.createElement('option');
                option.value = group.id;
                option.textContent = group.name;
                select.appendChild(option);
            });
        })
        .catch(error => {
            console.error('Error loading groups:', error);
        });
}

/**
 * Fetch template details for preview
 * @param {string} templateId - The ID of the template to fetch
 */
function fetchTemplateDetails(templateId) {
    fetch(`/api/email-templates/${templateId}`)
        .then(response => response.json())
        .then(template => {
            document.getElementById('previewSubject').textContent = template.subject;
            document.getElementById('previewBody').innerHTML = template.body;

            const placeholdersText = template.placeholders.length > 0
                ? template.placeholders.join(', ')
                : 'None';
            document.getElementById('previewPlaceholders').textContent = placeholdersText;

            document.getElementById('templatePreview').classList.remove('d-none');
        })
        .catch(error => {
            console.error('Error fetching template details:', error);
            hideTemplatePreview();
        });
}

/**
 * Hide the template preview section
 */
function hideTemplatePreview() {
    document.getElementById('templatePreview').classList.add('d-none');
}

/**
 * Submit the job to create a new email job
 */
function submitJob() {
    // Show spinner
    const spinner = document.getElementById('submitSpinner');
    const submitBtn = document.getElementById('submitJobBtn');
    spinner.classList.remove('d-none');
    submitBtn.disabled = true;

    // Collect data for submission
    const jobData = {
        name: document.getElementById('jobName').value,
        templateId: document.getElementById('templateSelect').value,
        fromEmail: document.getElementById('fromEmailSelect').value,
        cc: document.getElementById('ccInput').value.split(',').map(e => e.trim()).filter(e => e),
        bcc: document.getElementById('bccInput').value.split(',').map(e => e.trim()).filter(e => e),
        isPersonalized: false
    };

    const recipientType = document.querySelector('input[name="recipientType"]:checked').value;

    if (recipientType === 'basic') {
        // Individual recipients
        jobData.recipients = document.getElementById('recipients').value.split('\n')
            .map(e => e.trim())
            .filter(e => e);

        // Custom fields
        jobData.customFields = {};
        document.querySelectorAll('.custom-field-row').forEach(row => {
            const key = row.querySelector('.custom-field-key').value.trim();
            const value = row.querySelector('.custom-field-value').value.trim();
            if (key) {
                jobData.customFields[key] = value;
            }
        });
    } else {
        // Group
        jobData.groupId = document.getElementById('groupSelect').value;
        jobData.isPersonalized = true;
    }

    // Generate request verification token
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    // Send the data
    fetch('/api/jobs', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(jobData)
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Success - reload the page to show the new job
            window.location.href = '/jobs';
        })
        .catch(error => {
            console.error('Error creating job:', error);
            alert('There was a problem creating the job. Please try again.');

            // Hide spinner
            spinner.classList.add('d-none');
            submitBtn.disabled = false;
        });
}

/**
 * Show the delete confirmation dialog
 * @param {string} jobId - The ID of the job to delete
 */
function showDeleteConfirmation(jobId) {
    const modal = new bootstrap.Modal(document.getElementById('deleteJobModal'));
    document.getElementById('confirmDeleteButton').setAttribute('data-job-id', jobId);
    modal.show();
}

/**
 * Confirm and execute job deletion
 */
function confirmDelete() {
    const button = document.getElementById('confirmDeleteButton');
    const jobId = button.getAttribute('data-job-id');

    if (!jobId) return;

    // Generate request verification token
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    // Send delete request
    fetch(`/api/jobs/${jobId}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            // Close modal and reload page
            bootstrap.Modal.getInstance(document.getElementById('deleteJobModal')).hide();
            window.location.reload();
        })
        .catch(error => {
            console.error('Error deleting job:', error);
            alert('There was a problem deleting the job. Please try again.');
        });
}

/**
 * Retry a failed job
 * @param {string} jobId - The ID of the job to retry
 */
/**
 * Retry a failed job
 * @param {string} jobId - The ID of the job to retry
 */
function retryJob(jobId) {
    // Generate request verification token
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    fetch(`/api/jobs/${jobId}/retry`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Success - reload the page to show updated job status
            window.location.reload();
        })
        .catch(error => {
            console.error('Error retrying job:', error);
            alert('There was a problem retrying the job. Please try again.');
        });
}
