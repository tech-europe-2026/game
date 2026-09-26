const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');
const scoreElement = document.getElementById('score');
const timeElement = document.getElementById('time');
const gameOverScreen = document.getElementById('game-over');
const finalScoreElement = document.getElementById('final-score');
const restartBtn = document.getElementById('restart-btn');

let score = 0;
let timeLeft = 30;
let gameInterval;
let timerInterval;
let isPlaying = false;

// Target object
const target = {
    x: 0,
    y: 0,
    radius: 30,
    dx: 4,
    dy: 4,
    color: '#ff6b6b'
};

function initGame() {
    score = 0;
    timeLeft = 30;
    scoreElement.textContent = score;
    timeElement.textContent = timeLeft;
    gameOverScreen.classList.add('hidden');
    
    // Spawn target randomly
    spawnTarget();
    
    isPlaying = true;
    
    // Clear old intervals if they exist
    clearInterval(timerInterval);
    cancelAnimationFrame(gameInterval);
    
    // Start timers and game loop
    timerInterval = setInterval(updateTimer, 1000);
    gameLoop();
}

function spawnTarget() {
    target.radius = Math.max(15, 40 - (score * 0.5)); // Gets smaller as you score
    target.x = Math.random() * (canvas.width - target.radius * 2) + target.radius;
    target.y = Math.random() * (canvas.height - target.radius * 2) + target.radius;
    
    // Increase speed as score goes up
    const speed = 3 + (score * 0.2);
    target.dx = (Math.random() > 0.5 ? 1 : -1) * speed;
    target.dy = (Math.random() > 0.5 ? 1 : -1) * speed;
}

function updateTimer() {
    timeLeft--;
    timeElement.textContent = timeLeft;
    
    if (timeLeft <= 0) {
        endGame();
    }
}

function endGame() {
    isPlaying = false;
    clearInterval(timerInterval);
    cancelAnimationFrame(gameInterval);
    
    finalScoreElement.textContent = score;
    gameOverScreen.classList.remove('hidden');
}

function drawTarget() {
    ctx.beginPath();
    ctx.arc(target.x, target.y, target.radius, 0, Math.PI * 2);
    ctx.fillStyle = target.color;
    ctx.fill();
    ctx.closePath();
}

function updateTarget() {
    target.x += target.dx;
    target.y += target.dy;
    
    // Bounce off walls
    if (target.x + target.radius > canvas.width || target.x - target.radius < 0) {
        target.dx = -target.dx;
    }
    if (target.y + target.radius > canvas.height || target.y - target.radius < 0) {
        target.dy = -target.dy;
    }
}

function gameLoop() {
    if (!isPlaying) return;
    
    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    updateTarget();
    drawTarget();
    
    gameInterval = requestAnimationFrame(gameLoop);
}

// Handle clicks
canvas.addEventListener('mousedown', (e) => {
    if (!isPlaying) return;
    
    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;
    
    // Calculate distance between click and target center
    const distance = Math.sqrt(
        Math.pow(mouseX - target.x, 2) + 
        Math.pow(mouseY - target.y, 2)
    );
    
    // If clicked inside the target
    if (distance <= target.radius) {
        score++;
        scoreElement.textContent = score;
        spawnTarget(); // Respawn immediately
    }
});

// Event Listeners
restartBtn.addEventListener('click', initGame);

// Start game initially
initGame();
