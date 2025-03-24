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

document.querySelectorAll('.form-select').forEach(function (select) {
    var selectOptions = select.querySelectorAll('option');

    select.classList.add('hide-select');
    var wrapper = document.createElement('div');
    wrapper.className = 'select';
    select.parentNode.insertBefore(wrapper, select);
    wrapper.appendChild(select);

    var customSelect = document.createElement('div');
    customSelect.className = 'custom-select';
    customSelect.textContent = selectOptions[0].textContent;
    wrapper.appendChild(customSelect);

    var optionList = document.createElement('ul');
    optionList.className = 'select-options';
    wrapper.appendChild(optionList);

    selectOptions.forEach(function (option) {
        var listItem = document.createElement('li');
        listItem.textContent = option.textContent;
        listItem.setAttribute('rel', option.value);
        optionList.appendChild(listItem);

        listItem.addEventListener('click', function (e) {
            e.stopPropagation();
            customSelect.textContent = this.textContent;
            select.value = this.getAttribute('rel');
            optionList.style.display = 'none';
            customSelect.classList.remove('active');
        });
    });

    customSelect.addEventListener('click', function (e) {
        e.stopPropagation();
        document.querySelectorAll('.custom-select.active').forEach(function (activeSelect) {
            if (activeSelect !== customSelect) {
                activeSelect.classList.remove('active');
                activeSelect.nextElementSibling.style.display = 'none';
            }
        });
        customSelect.classList.toggle('active');
        optionList.style.display = optionList.style.display === 'block' ? 'none' : 'block';
    });

    document.addEventListener('click', function () {
        customSelect.classList.remove('active');
        optionList.style.display = 'none';
    });
});

const renderChart = (containerId, chartData) => {
    Highcharts.chart(containerId, {
        chart: {
            height: 300,
            plotBackgroundColor: null,
            plotBorderWidth: 0,
            plotShadow: false

        },
        title: {
            text: '8',
            align: 'center',
            verticalAlign: 'middle',
            style: {
                fontSize: '40px',
                fontWeight: 'bold',
                color: '#333'
            }
        },
        tooltip: {
            pointFormat: '<b>{point.name}: </b>{point.percentage:.1f}%'
        },
        plotOptions: {
            pie: {
                dataLabels: {
                    enabled: false
                },
                startAngle: 0,
                endAngle: 360,
                center: ['50%', '50%'],
                size: '100%'
            }
        },
        series: [{
            type: 'pie',
            innerSize: '80%',
            data: chartData

        }],
        credits: {
            enabled: false // Ẩn dòng text "Highcharts.com"
        }
    });
};
const data1 = [
    { name: 'Processing', y: 8, color: '#FFA500' }, // Orange for processing
    { name: 'Completed', y: 2, color: '#4CAF50' }  // Green for completed
];

renderChart('task-container-chart', data1);
renderChart('task-container-chart2', data1);
renderChart('task-container-chart3', data1);