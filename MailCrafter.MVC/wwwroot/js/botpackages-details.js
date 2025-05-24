/**
 * botpackages-details.js
 * 
 * JavaScript for the Bot Package details page, handling:
 * - Viewing robot details from deployment history
 * - Activating/deactivating packages
 * - Deleting packages
 */

document.addEventListener('DOMContentLoaded', function () {
    // Get the package ID from the URL
    const packageId = window.location.pathname.split('/').pop();

    // Initialize all modals
    const modalElements = document.querySelectorAll('.modal');
    modalElements.forEach(element => {
        new bootstrap.Modal(element);
    });

    // ===== Fetch Robot Info =====
    document.querySelectorAll('.fetch-robot-info').forEach(button => {
        button.addEventListener('click', function () {
            const robotId = this.getAttribute('data-robot-id');
            fetchRobotDetails(robotId);
        });
    });

    // ===== Deactivate Package =====
    const deactivatePackageBtn = document.getElementById('deactivatePackageBtn');
    if (deactivatePackageBtn) {
        deactivatePackageBtn.addEventListener('click', function () {
            const modal = new bootstrap.Modal(document.getElementById('deactivatePackageModal'));
            modal.show();
        });
    }

    // ===== Confirm Deactivate =====
    const confirmDeactivateBtn = document.getElementById('confirmDeactivateBtn');
    if (confirmDeactivateBtn) {
        confirmDeactivateBtn.addEventListener('click', function () {
            togglePackageStatus(packageId, false);
        });
    }

    // ===== Activate Package =====
    const activatePackageBtn = document.getElementById('activatePackageBtn');
    if (activatePackageBtn) {
        activatePackageBtn.addEventListener('click', function () {
            const modal = new bootstrap.Modal(document.getElementById('activatePackageModal'));
            modal.show();
        });
    }

    // ===== Confirm Activate =====
    const confirmActivateBtn = document.getElementById('confirmActivateBtn');
    if (confirmActivateBtn) {
        confirmActivateBtn.addEventListener('click', function () {
            togglePackageStatus(packageId, true);
        });
    }

    // ===== Delete Package =====
    const deletePackageBtn = document.getElementById('deletePackageBtn');
    if (deletePackageBtn) {
        deletePackageBtn.addEventListener('click', function () {
            const modal = new bootstrap.Modal(document.getElementById('deletePackageModal'));
            modal.show();
        });
    }

    // ===== Confirm Delete =====
    const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', function () {
            deletePackage(packageId);
        });
    }
});

/**
 * Fetch and display robot details in a modal
 * @param {string} robotId - The ID of the robot to fetch details for
 */
function fetchRobotDetails(robotId) {
    const modal = new bootstrap.Modal(document.getElementById('robotDetailsModal'));
    const contentDiv = document.getElementById('robotDetailsContent');

    // Show loading spinner
    contentDiv.innerHTML = `
        <div class="text-center">
            <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    `;

    modal.show();

    // Fetch robot details from API
    fetch(`/api/robots/${robotId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(response.status === 404 ? 'Robot not found' : 'Failed to fetch robot details');
            }
            return response.json();
        })
        .then(robot => {
            // Update modal content with robot details
            contentDiv.innerHTML = `
                <div class="mb-3">
                    <label class="fw-bold">Machine Name:</label>
                    <p>${robot.machineName}</p>
                </div>
                <div class="mb-3">
                    <label class="fw-bold">Status:</label>
                    <p>${robot.isConnected ?
                    '<span class="badge bg-success">Connected</span>' :
                    '<span class="badge bg-secondary">Disconnected</span>'}</p>
                </div>
                <div class="mb-3">
                    <label class="fw-bold">Last Seen:</label>
                    <p>${formatDateTime(robot.lastSeen)}</p>
                </div>
                <div class="mb-3">
                    <label class="fw-bold">Agent Version:</label>
                    <p>${robot.agentVersion || 'Unknown'}</p>
                </div>
                <div class="mb-3">
                    <label class="fw-bold">OS Info:</label>
                    <p>${robot.osInfo || 'Unknown'}</p>
                </div>
                <div class="mb-3">
                    <label class="fw-bold">Username:</label>
                    <p>${robot.userName || 'Unknown'}</p>
                </div>
                <div class="mt-3">
                    <a href="/robots/details/${robot.id}" class="btn btn-primary" target="_blank">
                        <i class="fas fa-external-link-alt"></i> View Full Details
                    </a>
                </div>
            `;
        })
        .catch(error => {
            contentDiv.innerHTML = `
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-circle"></i> Error: ${error.message}
                </div>
                <p>Could not retrieve robot details. The robot may have been deleted.</p>
            `;
            console.error('Error fetching robot details:', error);
        });
}

/**
 * Toggle the active status of a bot package
 * @param {string} packageId - The ID of the package to toggle status for
 * @param {boolean} activate - True to activate, false to deactivate
 */
function togglePackageStatus(packageId, activate) {
    const action = activate ? 'activate' : 'deactivate';
    const modalId = activate ? 'activatePackageModal' : 'deactivatePackageModal';
    const btnId = activate ? 'confirmActivateBtn' : 'confirmDeactivateBtn';

    const button = document.getElementById(btnId);
    const originalContent = button.innerHTML;

    // Show loading state
    button.disabled = true;
    button.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...`;

    // Send request to API to toggle package status
    fetch(`/api/botpackages/${packageId}/${action}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`Failed to ${action} package`);
            }
            return response.json();
        })
        .then(data => {
            // Close the modal and reload the page to show updated status
            bootstrap.Modal.getInstance(document.getElementById(modalId)).hide();
            window.location.reload();
        })
        .catch(error => {
            console.error(`Error ${action}ing package:`, error);
            button.innerHTML = originalContent;
            button.disabled = false;

            // Show error in the modal
            const modal = document.querySelector(`#${modalId} .modal-body`);
            const errorAlert = document.createElement('div');
            errorAlert.className = 'alert alert-danger mt-3';
            errorAlert.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${error.message}`;
            modal.appendChild(errorAlert);
        });
}

/**
 * Delete a bot package
 * @param {string} packageId - The ID of the package to delete
 */
function deletePackage(packageId) {
    const button = document.getElementById('confirmDeleteBtn');
    const originalContent = button.innerHTML;

    // Show loading state
    button.disabled = true;
    button.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Deleting...`;

    fetch(`/api/botpackages/${packageId}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to delete package');
            }
            return response.json();
        })
        .then(data => {
            // Redirect to the packages list page on success
            bootstrap.Modal.getInstance(document.getElementById('deletePackageModal')).hide();
            window.location.href = '/botpackages'; // Redirect to packages list
        })
        .catch(error => {
            console.error('Error deleting package:', error);
            button.innerHTML = originalContent;
            button.disabled = false;

            // Show error in the modal
            const modal = document.querySelector('#deletePackageModal .modal-body');
            const errorAlert = document.createElement('div');
            errorAlert.className = 'alert alert-danger mt-3';
            errorAlert.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${error.message}`;
            modal.appendChild(errorAlert);
        });
}

/**
 * Format a date/time string for display
 * @param {string} dateString - ISO date string
 * @returns {string} Formatted date/time
 */
function formatDateTime(dateString) {
    if (!dateString) return 'Never';

    const date = new Date(dateString);
    if (isNaN(date.getTime())) return 'Invalid date';

    return date.toLocaleString();
}
