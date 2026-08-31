window.focusElement = (element) => {
    if (element) element.focus();
};

// A small, dependency-free celebration for meaningful learning milestones.
window.quizCelebration = {
    show: (celebrationId, intensity = "pass") => {
        if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

        const storageKey = `quiz-celebration:${celebrationId}`;
        if (sessionStorage.getItem(storageKey)) return;
        sessionStorage.setItem(storageKey, "shown");

        const canvas = document.createElement("canvas");
        canvas.className = "confetti-canvas";
        canvas.setAttribute("aria-hidden", "true");
        document.body.appendChild(canvas);

        const context = canvas.getContext("2d");
        const perfect = intensity === "perfect";
        const colors = perfect
            ? ["#d6f06f", "#f4c95d", "#ffffff", "#2f8f63", "#f08b62"]
            : ["#d6f06f", "#2f8f63", "#ffffff", "#7ec8a3"];
        const particleCount = perfect ? 150 : 85;
        let width;
        let height;
        let animationFrame;
        const startedAt = performance.now();
        const duration = perfect ? 2200 : 1500;

        const resize = () => {
            const ratio = Math.min(window.devicePixelRatio || 1, 2);
            width = window.innerWidth;
            height = window.innerHeight;
            canvas.width = width * ratio;
            canvas.height = height * ratio;
            canvas.style.width = `${width}px`;
            canvas.style.height = `${height}px`;
            context.setTransform(ratio, 0, 0, ratio, 0, 0);
        };

        resize();
        window.addEventListener("resize", resize);

        const particles = Array.from({ length: particleCount }, (_, index) => ({
            x: perfect ? width / 2 + (Math.random() - .5) * 180 : Math.random() * width,
            y: perfect ? height * .38 : -20 - Math.random() * height * .25,
            velocityX: perfect ? (Math.random() - .5) * 14 : (Math.random() - .5) * 3,
            velocityY: perfect ? -7 - Math.random() * 8 : 3 + Math.random() * 5,
            gravity: perfect ? .25 : .13,
            rotation: Math.random() * Math.PI,
            rotationSpeed: (Math.random() - .5) * .25,
            width: 6 + Math.random() * 7,
            height: 4 + Math.random() * 6,
            color: colors[index % colors.length],
            opacity: 1
        }));

        const cleanup = () => {
            cancelAnimationFrame(animationFrame);
            window.removeEventListener("resize", resize);
            canvas.remove();
        };

        const draw = now => {
            const progress = (now - startedAt) / duration;
            context.clearRect(0, 0, width, height);

            particles.forEach(particle => {
                particle.velocityY += particle.gravity;
                particle.x += particle.velocityX;
                particle.y += particle.velocityY;
                particle.rotation += particle.rotationSpeed;
                particle.opacity = Math.max(0, 1 - Math.max(0, progress - .68) / .32);

                context.save();
                context.globalAlpha = particle.opacity;
                context.translate(particle.x, particle.y);
                context.rotate(particle.rotation);
                context.fillStyle = particle.color;
                context.fillRect(-particle.width / 2, -particle.height / 2, particle.width, particle.height);
                context.restore();
            });

            if (progress < 1) animationFrame = requestAnimationFrame(draw);
            else cleanup();
        };

        animationFrame = requestAnimationFrame(draw);
    }
};

// Chart.js rendering function for performance chart
window.renderPerformanceChart = (labels, scores) => {
    try {
        var ctx = document.getElementById('performanceChart');
        if (!ctx) {
            console.error('Canvas element with id "performanceChart" not found');
            return;
        }

        // Destroy existing chart if it exists
        if (window.performanceChartInstance) {
            window.performanceChartInstance.destroy();
        }

        // Create new chart
        window.performanceChartInstance = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Score (%)',
                    data: scores,
                    borderColor: 'rgba(54, 162, 235, 1)',
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3,
                    pointBackgroundColor: 'rgba(54, 162, 235, 1)',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        callbacks: {
                            label: function(context) {
                                return 'Score: ' + context.parsed.y.toFixed(1) + '%';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        display: true,
                        title: {
                            display: true,
                            text: 'Date'
                        },
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        display: true,
                        title: {
                            display: true,
                            text: 'Score (%)'
                        },
                        beginAtZero: true,
                        max: 100,
                        ticks: {
                            callback: function(value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.1)'
                        }
                    }
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                }
            }
        });
    } catch (error) {
        console.error('Error rendering performance chart:', error);
    }
};
