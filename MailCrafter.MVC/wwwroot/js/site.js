const showLoading = (show = false) => {
    if (show) {
        const layerDiv = document.createElement('div');
        layerDiv.id = 'loading';
        layerDiv.className = 'loading-container';
        const loadingDiv = document.createElement('div');
        loadingDiv.className = 'loading-tri-circular center';
        layerDiv.appendChild(loadingDiv);
        return document.body.appendChild(layerDiv);
    } else {
        const loading = document.getElementById('loading');
        return loading?.remove();
    }
};

async function registerUser(e) {
    showLoading(true);
    e.preventDefault();
    const username = document.getElementById('username').value;
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    // Clear previous errors and error classes
    document.getElementById('username').classList.remove('error');
    document.getElementById('email').classList.remove('error');
    document.getElementById('password').classList.remove('error');
    document.getElementById('username-error').textContent = '';
    document.getElementById('email-error').textContent = '';
    document.getElementById('password-error').textContent = '';

    const response = await fetch('/register', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            Username: username,
            Email: email,
            Password: password
        })
    });

    if (response.ok) {
        window.location.href = '/login';
    } else {
        const errorData = await response.json();
        console.log('Error data:', errorData); // Debug log

        if (errorData.errors) {
            const errors = errorData.errors;
            
            // Handle ModelState errors
            if (errors.Username) {
                document.getElementById('username-error').textContent = errors.Username.errors[0].errorMessage;
            }
            if (errors.Email) {
                document.getElementById('email-error').textContent = errors.Email[0].errorMessage;
            }
            if (errors.Password) {
                document.getElementById('password-error').textContent = errors.Password[0].errorMessage;
            }
        } else if (errorData.message) {
            // Handle general error message
            document.getElementById('username-error').textContent = errorData.message;
        }
    }
    showLoading(false);
}

async function login(event) {
    showLoading(true);
    event.preventDefault(); // Prevent the form from submitting the traditional way

    const username = document.getElementById('Username').value;
    const password = document.getElementById('Password').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const response = await fetch('/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ Username: username, Password: password })
    });

    if (response.ok) {
        window.location.href = '/';
    } else {
        const errorData = await response.json();
        document.getElementById('login-error').innerText = errorData.message || 'Invalid login attempt.';
    }
    showLoading(false);
}

function logout() {
    fetch('/logout', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    }).then(response => {
        if (response.ok) {
            window.location.href = '/login';
        }
    }).catch(error => {
        console.error('Error:', error);
    });
}

async function addEmailAccount() {
    const email = document.getElementById('email').value;
    const alias = document.getElementById('alias').value;
    const appPassword = document.getElementById('appPassword').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const response = await fetch('/management/email-accounts/add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ email, alias, appPassword })
    });

    if (response.ok) {
        window.location.href = '/management/email-accounts';
    } else {
        const errorData = await response.json();
        document.getElementById('email-error').innerText = errorData.errors.Email || '';
        document.getElementById('alias-error').innerText = errorData.errors.Alias || '';
        document.getElementById('appPassword-error').innerText = errorData.errors.AppPassword || '';
    }
}

const switchToAddEmailAccount = () => {
    return window.location.href = '/management/email-accounts/add';
}

const viewPassword = () => {
    const password = document.getElementById('Password');
    password.type == 'password' ? password.type = 'text' : password.type = 'password';
}

function isValidEmail(email) {
    return email.includes('@') && email.includes('.') && email.indexOf('@') < email.lastIndexOf('.');
}

// Edit Profile Functionality
document.addEventListener('DOMContentLoaded', function() {
    // Toggle password visibility for all password fields
    const togglePasswordButtons = document.querySelectorAll('[id^="toggle"]');
    togglePasswordButtons.forEach(button => {
        button.addEventListener('click', function() {
            const inputId = this.id.replace('toggle', '').toLowerCase();
            const input = document.getElementById(inputId);
            const type = input.type === 'password' ? 'text' : 'password';
            input.type = type;
            this.querySelector('i').classList.toggle('fa-eye');
            this.querySelector('i').classList.toggle('fa-eye-slash');
        });
    });

    // Handle profile update
    const saveProfileButton = document.getElementById('saveProfileChanges');
    if (saveProfileButton) {
        saveProfileButton.addEventListener('click', async function() {
            const form = document.getElementById('editProfileForm');
            const formData = new FormData(form);
            const data = {
                username: formData.get('username'),
                email: formData.get('email'),
                currentPassword: formData.get('currentPassword'),
                newPassword: formData.get('newPassword'),
                confirmPassword: formData.get('confirmPassword')
            };

            // Validate passwords match if new password is provided
            if (data.newPassword && data.newPassword !== data.confirmPassword) {
                showToast('New passwords do not match', 'error');
                return;
            }

            // Show loading state
            const saveButton = this;
            const originalText = saveButton.innerHTML;
            saveButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
            saveButton.disabled = true;

            try {
                const response = await fetch('/api/profile/update', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                });

                const result = await response.json();

                if (response.ok) {
                    showToast('Profile updated successfully', 'success');
                    
                    // Update the username in the sidebar
                    const sidebarUsername = document.querySelector('.profile-section .username');
                    if (sidebarUsername) {
                        sidebarUsername.textContent = data.username;
                    }

                    // Update the form fields with new values
                    form.querySelector('[name="username"]').value = data.username;
                    form.querySelector('[name="email"]').value = data.email;
                    
                    // Clear password fields
                    form.querySelector('[name="currentPassword"]').value = '';
                    form.querySelector('[name="newPassword"]').value = '';
                    form.querySelector('[name="confirmPassword"]').value = '';

                    // Close the modal
                    const modal = bootstrap.Modal.getInstance(document.getElementById('editProfileModal'));
                    modal.hide();
                } else {
                    // Show error message
                    showToast(result.message || 'Failed to update profile', 'error');
                    
                    // If it's a password error, clear the password fields
                    if (result.message === 'Current password is incorrect') {
                        form.querySelector('[name="currentPassword"]').value = '';
                        form.querySelector('[name="newPassword"]').value = '';
                        form.querySelector('[name="confirmPassword"]').value = '';
                    }
                }
            } catch (error) {
                console.error('Error updating profile:', error);
                showToast('An error occurred while updating profile', 'error');
            } finally {
                // Reset button state
                saveButton.innerHTML = originalText;
                saveButton.disabled = false;
            }
        });
    }
});

// Toast notification function
function showToast(message, type = 'info') {
    // Remove any existing toasts
    const existingToasts = document.querySelectorAll('.toast-container');
    existingToasts.forEach(toast => toast.remove());

    // Create toast container if it doesn't exist
    let toastContainer = document.querySelector('.toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999'; // Ensure toast appears above modal
        document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : type} border-0`;
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');
    toastEl.style.zIndex = '9999'; // Ensure toast appears above modal

    // Create toast content
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    // Add toast to container
    toastContainer.appendChild(toastEl);

    // Initialize and show toast
    const toast = new bootstrap.Toast(toastEl, {
        autohide: true,
        delay: 5000
    });
    toast.show();
}



