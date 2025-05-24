/**
 * robots.js - JavaScript for Robot Management
 * This file handles robot creation, monitoring, status updates, and command operations
 */

document.addEventListener('DOMContentLoaded', function () {
    initializeRobotsPage();
});

/**
 * Initialize the Robots page
 */
function initializeRobotsPage() {
    setupEventHandlers();
}

/**
 * Set up all event handlers for the page
 */
function setupEventHandlers() {
    // Create Robot button
    document.getElementById('createRobotBtn').addEventListener('click', function () {
        const modal = new bootstrap.Modal(document.getElementById('createRobotModal'));
        document.getElementById('machineName').value = '';
        document.getElementById('machineName').classList.remove('is-invalid');
        document.getElementById('machineNameError').textContent = '';
        modal.show();
    });

    // Submit create robot form
    document.getElementById('submitRobotBtn').addEventListener('click', createRobot);

    // Copy Machine Key button
    document.getElementById('copyKeyBtn').addEventListener('click', copyMachineKey);

    // Command type change
    document.getElementById('commandType').addEventListener('change', function () {
        const payloadContainer = document.getElementById('payloadContainer');
        if (this.value === 'execute') {
            payloadContainer.style.display = 'block';
        } else {
            payloadContainer.style.display = 'none';
        }
    });

    // Set up delegated event handlers for dynamic elements
    document.addEventListener('click', function (e) {
        // View Robot Details
        if (e.target.closest('.view-robot')) {
            const button = e.target.closest('.view-robot');
            const robotId = button.getAttribute('data-robot-id');
            viewRobotDetails(robotId);
        }

        // Send Command
        if (e.target.closest('.send-command')) {
            const button = e.target.closest('.send-command');
            const robotId = button.getAttribute('data-robot-id');
            showSendCommandModal(robotId);
        }

        // Delete Robot
        if (e.target.closest('.delete-robot')) {
            const button = e.target.closest('.delete-robot');
            const robotId = button.getAttribute('data-robot-id');
            const robotName = button.getAttribute('data-robot-name');
            showDeleteConfirmation(robotId, robotName);
        }
    });

    // Send command button in modal
    document.getElementById('sendCommandBtn').addEventListener('click', sendCommand);

    // Delete confirmation button
    document.getElementById('confirmDeleteBtn').addEventListener('click', deleteRobot);
}

/**
 * Create a new robot
 */
function createRobot() {
    const machineName = document.getElementById('machineName').value.trim();

    // Validate
    if (!machineName) {
        document.getElementById('machineName').classList.add('is-invalid');
        document.getElementById('machineNameError').textContent = 'Machine name is required';
        return;
    }

    // Show loading state
    const submitBtn = document.getElementById('submitRobotBtn');
    const spinner = document.getElementById('createSpinner');
    submitBtn.disabled = true;
    spinner.classList.remove('d-none');

    // Send the request
    fetch('/api/robots/create', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ machineName: machineName })
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(data => {
                    throw new Error(data.message || 'Failed to create robot');
                });
            }
            return response.json();
        })
        .then(data => {
            // Hide create modal
            bootstrap.Modal.getInstance(document.getElementById('createRobotModal')).hide();

            // Show success modal with created robot details
            document.getElementById('createdMachineName').value = data.machineName;
            document.getElementById('createdMachineKey').value = data.machineKey;

            const successModal = new bootstrap.Modal(document.getElementById('robotCreatedModal'));
            successModal.show();

            // Refresh page after delay
            setTimeout(() => window.location.reload(), 5000);
        })
        .catch(error => {
            document.getElementById('machineName').classList.add('is-invalid');
            document.getElementById('machineNameError').textContent = error.message || 'Error creating robot';
        })
        .finally(() => {
            // Reset loading state
            submitBtn.disabled = false;
            spinner.classList.add('d-none');
        });
}

/**
 * Copy the machine key to clipboard
 */
function copyMachineKey() {
    const keyElement = document.getElementById('createdMachineKey');
    keyElement.select();
    document.execCommand('copy');

    // Visual feedback
    const copyBtn = document.getElementById('copyKeyBtn');
    const originalHTML = copyBtn.innerHTML;
    copyBtn.innerHTML = '<i class="fas fa-check"></i> Copied!';

    setTimeout(() => {
        copyBtn.innerHTML = originalHTML;
    }, 2000);
}

/**
 * View details for a specific robot
 * @param {string} robotId - The ID of the robot to view
 */
function viewRobotDetails(robotId) {
    fetch(`/api/robots/${robotId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to fetch robot details');
            }
            return response.json();
        })
        .then(robot => {
            // Populate the details modal
            document.getElementById('detailMachineName').textContent = robot.machineName;

            const statusEl = document.getElementById('detailStatus');
            if (robot.isConnected) {
                statusEl.innerHTML = '<span class="badge bg-success">Connected</span>';
            } else {
                statusEl.innerHTML = '<span class="badge bg-secondary">Disconnected</span>';
            }

            document.getElementById('detailLastSeen').textContent = formatDateTime(robot.lastSeen);
            document.getElementById('detailAgentVersion').textContent = robot.agentVersion || 'Unknown';
            document.getElementById('detailOsInfo').textContent = robot.osInfo || 'Unknown';
            document.getElementById('detailUsername').textContent = robot.userName || 'Unknown';

            // Show the modal
            const modal = new bootstrap.Modal(document.getElementById('robotDetailsModal'));
            modal.show();
        })
        .catch(error => {
            console.error('Error fetching robot details:', error);
            alert('Could not load robot details. Please try again.');
        });
}

/**
 * Show the send command modal for a specific robot
 * @param {string} robotId - The ID of the robot to send a command to
 */
function showSendCommandModal(robotId) {
    // Reset form
    document.getElementById('commandType').value = 'status';
    document.getElementById('payloadContainer').style.display = 'none';
    document.getElementById('commandPayload').value = '';

    // Store the robot ID on the send button for later use
    document.getElementById('sendCommandBtn').setAttribute('data-robot-id', robotId);

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('sendCommandModal'));
    modal.show();
}

/**
 * Send a command to a robot
 */
function sendCommand() {
    const btn = document.getElementById('sendCommandBtn');
    const robotId = btn.getAttribute('data-robot-id');
    const commandType = document.getElementById('commandType').value;
    const spinner = document.getElementById('commandSpinner');

    // Prepare command data
    const commandData = {
        type: commandType,
        robotId: robotId
    };

    // Add payload if it's an execute command
    if (commandType === 'execute') {
        try {
            const payloadText = document.getElementById('commandPayload').value.trim();
            if (payloadText) {
                // Try to parse as JSON, but fall back to string if not valid JSON
                try {
                    commandData.payload = JSON.parse(payloadText);
                } catch {
                    commandData.payload = payloadText;
                }
            }
        } catch (e) {
            alert('Invalid payload format');
            return;
        }
    }

    // Show loading state
    btn.disabled = true;
    spinner.classList.remove('d-none');

    // Send the command
    fetch(`/api/robots/command/${robotId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(commandData)
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(data => {
                    throw new Error(data.message || 'Failed to send command');
                });
            }
            return response.json();
        })
        .then(data => {
            bootstrap.Modal.getInstance(document.getElementById('sendCommandModal')).hide();
            alert('Command sent successfully!');
        })
        .catch(error => {
            console.error('Error sending command:', error);
            alert('Error sending command: ' + error.message);
        })
        .finally(() => {
            // Reset loading state
            btn.disabled = false;
            spinner.classList.add('d-none');
        });
}

/**
 * Show the delete confirmation modal
 * @param {string} robotId - The ID of the robot to delete
 * @param {string} robotName - The name of the robot to delete
 */
function showDeleteConfirmation(robotId, robotName) {
    document.getElementById('deleteRobotName').textContent = robotName;
    document.getElementById('confirmDeleteBtn').setAttribute('data-robot-id', robotId);

    const modal = new bootstrap.Modal(document.getElementById('deleteRobotModal'));
    modal.show();
}

/**
 * Delete a robot
 */
function deleteRobot() {
    const btn = document.getElementById('confirmDeleteBtn');
    const robotId = btn.getAttribute('data-robot-id');
    const spinner = document.getElementById('deleteSpinner');

    // Show loading state
    btn.disabled = true;
    spinner.classList.remove('d-none');

    fetch(`/api/robots/${robotId}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to delete robot');
            }

            // Hide modal and reload page
            bootstrap.Modal.getInstance(document.getElementById('deleteRobotModal')).hide();
            window.location.reload();
        })
        .catch(error => {
            console.error('Error deleting robot:', error);
            alert('Error deleting robot: ' + error.message);
        })
        .finally(() => {
            // Reset loading state
            btn.disabled = false;
            spinner.classList.add('d-none');
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

/**
 * Check if a robot is online by polling its status
 * @param {string} machineKey - The machine key of the robot to check
 * @returns {Promise<boolean>} Promise resolving to the connection status
 */
function checkRobotStatus(machineKey) {
    return fetch(`/api/robots/status?machineKey=${encodeURIComponent(machineKey)}`)
        .then(response => {
            if (!response.ok) return { isConnected: false };
            return response.json();
        })
        .then(data => data.isConnected)
        .catch(() => false);
}

/**
 * Error handling for fetch operations
 * @param {Response} response - Fetch response object
 * @returns {Promise} The response JSON or an error
 */
function handleFetchErrors(response) {
    if (!response.ok) {
        return response.json().then(data => {
            throw new Error(data.message || response.statusText);
        });
    }
    return response.json();
}
