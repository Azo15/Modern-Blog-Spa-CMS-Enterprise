
document.addEventListener('DOMContentLoaded', function () {
    const containers = document.querySelectorAll('.reservation-timer-container');

    // Thresholds
    const YELLOW_THRESHOLD_MS = 5 * 60 * 1000; // 5 dakika
    const RED_THRESHOLD_MS = 15 * 60 * 1000;    // 10 dakika

    function interpolateColor(color1, color2, factor) {
        // color1 & color2 = [r,g,b], factor 0-1 arası
        return [
            Math.round(color1[0] + factor * (color2[0] - color1[0])),
            Math.round(color1[1] + factor * (color2[1] - color1[1])),
            Math.round(color1[2] + factor * (color2[2] - color1[2]))
        ];
    }

    function updateTimers() {
        containers.forEach(container => {
            const parentTd = container.closest('td');
            const createdDateString = parentTd.dataset.createdDate;
            const createdDate = new Date(createdDateString);
            const now = new Date();

            const diffMs = now - createdDate;

            // HH:mm:ss
            const hours = Math.floor(diffMs / (1000 * 60 * 60));
            const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((diffMs % (1000 * 60)) / 1000);

            container.querySelector('.reservation-timer-text').textContent =
                `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;

            // Renk geçişi
            let color;
            if (diffMs <= YELLOW_THRESHOLD_MS) {
                let factor = diffMs / YELLOW_THRESHOLD_MS;
                color = interpolateColor([76, 175, 80], [255, 193, 7], factor);
            } else if (diffMs <= RED_THRESHOLD_MS) {
                let factor = (diffMs - YELLOW_THRESHOLD_MS) / (RED_THRESHOLD_MS - YELLOW_THRESHOLD_MS);
                color = interpolateColor([255, 193, 7], [244, 67, 54], factor);
            } else {
                color = [244, 67, 54];
            }

            const rgb = `rgb(${color[0]}, ${color[1]}, ${color[2]})`;
            container.style.backgroundColor = rgb;

            // 🔥 10 dakikadan sonra sadece box-shadow + büyüyüp küçülme
            if (diffMs >= 15 * 60 * 1000) {
                container.classList.add("pulse");
                container.style.setProperty("--pulse-color", rgb); // box-shadow rengi arka planla aynı
            } else {
                container.classList.remove("pulse");
                container.style.boxShadow = "none"; // normalde gölge olmasın
            }
        });
    }


    updateTimers();
    setInterval(updateTimers, 1000);
});