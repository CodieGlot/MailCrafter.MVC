async function login(event) {
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



