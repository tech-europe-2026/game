import kaboom from "https://unpkg.com/kaboom@3000.1.17/dist/kaboom.mjs";

kaboom({
    root: document.getElementById("game-view"),
    width: 800,
    height: 600,
    background: [15, 15, 15],
});

// Player states, cropped from assets/states.jpeg (enemies are drawn as rects)
loadSprite("ball_idle", "/assets/sprites/idle.png");
loadSprite("ball_spin", "/assets/sprites/spin.png");
loadSprite("ball_bounce", "/assets/sprites/bounce.png");
loadSprite("ball_teleport", "/assets/sprites/teleport.png");
loadSprite("ball_stun", "/assets/sprites/stun.png");

const BOUNCE_STATE_DURATION = 0.15; // how long the impact pose holds after landing

// ==========================================
// SHARED HELPERS
// ==========================================

// The automated test subject: always runs forward, jumps on ground contact.
// Its sprite cycles between states based on time since the last ground contact:
// airborne -> "spin", just landed -> "bounce" (brief), otherwise -> "idle".
function addPlayer() {
    const player = add([
        sprite("ball_idle"),
        pos(50, 430),
        area(),
        body(),
        color(255, 255, 255),
    ]);

    player.scale = vec2(window.gameConfig.playerScale);

    let wasGrounded = true;
    let landedAt = -Infinity;
    let curState = "ball_idle";

    const setState = (state) => {
        if (state === curState) return;
        curState = state;
        player.use(sprite(state));
    };

    // Automation Logic! No keyboards allowed.
    player.onUpdate(() => {
        player.move(window.gameConfig.playerSpeed, 0);

        const grounded = player.isGrounded();
        if (grounded) {
            if (!wasGrounded) landedAt = time();
            player.jump(window.gameConfig.jumpForce);
        }
        wasGrounded = grounded;

        if (!grounded) {
            setState("ball_spin");
        } else if (time() - landedAt < BOUNCE_STATE_DURATION) {
            setState("ball_bounce");
        } else {
            setState("ball_idle");
        }
    });

    return player;
}

function addExit() {
    add([ rect(60, 80), pos(700, 420), area(), color(50, 255, 50), "goal" ]);
    add([ text("EXIT", { size: 16 }), pos(705, 390), color(50, 255, 50) ]);
}

// Catch-all kill zone below the world
function addBounds() {
    add([ rect(width(), 20), pos(0, height() + 20), area(), "pit" ]);
}

function wirePlayer(player) {
    player.onCollide("goal", () => {
        window.stopSimulation(true);
        go("win");
    });
    player.onCollide("pit", () => {
        shake(10);
        window.stopSimulation(false);
        go("lose", "You fell out of bounds!");
    });
}

// ==========================================
// SCENES
// ==========================================

// Idle screen — shown while no simulation is running
scene("game", () => {
    add([
        text("Tweak the variables\nand click RUN_SIMULATION()", { align: "center", size: 24 }),
        pos(width() / 2, height() / 2),
        anchor("center"),
        color(200, 200, 200)
    ]);
});

// ==========================================
// LEVEL 01 — WALL JUMP
// ==========================================
scene("wall", () => {
    setGravity(window.gameConfig.gravity);

    // The main floor
    add([ rect(width(), 40), pos(0, 500), area(), body({ isStatic: true }), color(80, 80, 80) ]);

    // A tall wall that the player must jump over
    // Default jump force (600) won't clear this with default gravity (1600)
    add([ rect(40, 150), pos(350, 350), area(), body({ isStatic: true }), color(150, 50, 50) ]);

    addBounds();
    addExit();

    wirePlayer(addPlayer());
});

// Win Scene
scene("win", () => {
    add([
        sprite("ball_teleport"),
        pos(width() / 2, height() / 2 - 100),
        anchor("center"),
        scale(1.5),
    ]);
    add([
        text("SIMULATION SUCCESS!\n\nYou found the perfect variables.\nPress SPACE to reset.", { align: "center", size: 24 }),
        pos(width() / 2, height() / 2 + 60),
        anchor("center"),
        color(50, 255, 50)
    ]);
    onKeyPress("space", () => {
        window.gameConfig.simulationRunning = false;
        go("game")
    });
});

// Lose Scene
scene("lose", (reason) => {
    add([
        sprite("ball_stun"),
        pos(width() / 2, height() / 2 - 100),
        anchor("center"),
        scale(1.5),
    ]);
    add([
        text(`SIMULATION FAILED\n\n${reason}\n\nPress SPACE to edit variables`, { align: "center", size: 24 }),
        pos(width() / 2, height() / 2 + 60),
        anchor("center"),
        color(255, 50, 50)
    ]);
    onKeyPress("space", () => {
        window.gameConfig.simulationRunning = false;
        go("game");
    });
});

// ==========================================
// GLOBAL EVENT LISTENERS
// ==========================================
document.addEventListener('startSimulation', () => {
    go("wall");
});

document.addEventListener("abortSimulation", () => {
    window.gameConfig.simulationRunning = false;
    go("game");
});

go("game");
