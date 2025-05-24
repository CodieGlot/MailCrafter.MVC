// Email form handler
document.addEventListener('DOMContentLoaded', function () {
    const templateSelect = document.getElementById('TemplateId');
    if (templateSelect) {
        templateSelect.addEventListener('change', function() {
            const templateId = this.value;
            if (templateId) {
                // Fetch template details
                fetch(`/api/email-templates/${templateId}`)
                    .then(response => response.json())
                    .then(data => {
                        // Update preview
                        document.getElementById('previewSubject').innerText = data.subject;
                        document.getElementById('previewBody').innerHTML = data.body;
                        document.getElementById('templatePreview').classList.remove('d-none');
                    })
                    .catch(error => {
                        console.error('Error fetching template:', error);
                    });
            } else {
                document.getElementById('templatePreview').classList.add('d-none');
            }
        });
    }

    // Process comma-separated emails to hidden inputs on form submit
    const form = document.querySelector('form#sendEmailForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Clear any previously created inputs
            document.querySelectorAll('input[name="CC"]').forEach(el => el.remove());
            document.querySelectorAll('input[name="Bcc"]').forEach(el => el.remove());
            
            let isValid = true;
            
            // Process CC field
            const ccInput = document.getElementById('ccInput');
            if (ccInput && ccInput.value) {
                const ccEmails = ccInput.value.split(',')
                    .map(email => email.trim())
                    .filter(email => email);
                
                // Create hidden inputs for each email
                for (let i = 0; i < ccEmails.length; i++) {
                    const email = ccEmails[i];
                    // Basic validation - must contain @ and .
                    if (email.indexOf('@') === -1 || email.indexOf('.') === -1) {
                        alert('Please enter valid email addresses in the CC field');
                        ccInput.focus();
                        isValid = false;
                        break;
                    }
                    
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = 'CC';
                    input.value = email;
                    form.appendChild(input);
                }
            }
            
            if (!isValid) return;
            
            // Process BCC field
            const bccInput = document.getElementById('bccInput');
            if (bccInput && bccInput.value) {
                const bccEmails = bccInput.value.split(',')
                    .map(email => email.trim())
                    .filter(email => email);
                
                // Create hidden inputs for each email
                for (let i = 0; i < bccEmails.length; i++) {
                    const email = bccEmails[i];
                    // Basic validation - must contain @ and .
                    if (email.indexOf('@') === -1 || email.indexOf('.') === -1) {
                        alert('Please enter valid email addresses in the BCC field');
                        bccInput.focus();
                        isValid = false;
                        break;
                    }
                    
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = 'Bcc';
                    input.value = email;
                    form.appendChild(input);
                }
            }
            
            if (!isValid) return;
            
            // Show loading state
            const submitBtn = document.getElementById('submitButton');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Sending...';
            }
            
            // Submit the form
            form.submit();
        });
    }
}); 