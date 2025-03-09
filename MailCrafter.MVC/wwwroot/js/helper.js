const showToastMessage = ({type, message}) => {
    const toastContainer = document.querySelector('.toast-container');
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.innerHTML = `
        <div class="toast-content">
        ${type === 'error' ?
            `<i class="fa-solid fa-exclamation error"></i>` :
            `<i class="fas fa-solid fa-check check"></i>`}

            <div class="message">
                <span class="text text-1">${type === 'error' ? 'Error' : 'Success'}</span>
                <span class="text text-2">${message}</span>
            </div>
        </div>
        <i class="fa-solid fa-xmark close"></i>
        <div class="progress"></div>
    `
    toastContainer.appendChild(toast);
    const closeIcon = toast.querySelector(".close");
    const progress = toast.querySelector(".progress");
    progress.classList.add('active');

    const removeToast = () => {
        toast.classList.add('remove')
        setTimeout(() => {
            toast.remove();
        }, 500);
    }

    const time1 = setTimeout(removeToast, 5000);

    closeIcon.addEventListener("click", () => {
        clearTimeout(time1);
        removeToast();
    });
}