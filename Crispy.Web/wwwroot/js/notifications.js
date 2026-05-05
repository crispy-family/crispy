"use strict";

// Підключення до хабу
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Метод, який буде викликатися сервером (BackgroundService)
connection.on("ReceiveNotification", function (message) {
    showNotificationToast(message);
});

// Запуск з'єднання
connection.start().then(function () {
    console.log("SignalR підключено.");
}).catch(function (err) {
    return console.error(err.toString());
});

// Проста функція для відображення Bootstrap Toast (спливаючого повідомлення)
function showNotificationToast(message) {
    // Створюємо HTML для Toast-у
    const toastContainer = document.getElementById("toast-container") || createToastContainer();

    const toastId = "toast-" + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white bg-primary border-0" role="alert" aria-live="assertive" aria-atomic="true">
          <div class="d-flex">
            <div class="toast-body">
              🔔 ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
          </div>
        </div>
    `;

    toastContainer.insertAdjacentHTML("beforeend", toastHtml);

    const toastElement = document.getElementById(toastId);
    const bsToast = new bootstrap.Toast(toastElement, { delay: 5000 });
    bsToast.show();
}

function createToastContainer() {
    const div = document.createElement("div");
    div.id = "toast-container";
    div.className = "toast-container position-fixed bottom-0 end-0 p-3";
    div.style.zIndex = "1055";
    document.body.appendChild(div);
    return div;
}