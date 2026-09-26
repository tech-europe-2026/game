# How to Launch the Game Locally

Since the game now uses ES Modules (`<script type="module">`) to import Kaboom.js, you **cannot** just double-click the `index.html` file to open it in your browser anymore. Browsers block module imports from `file://` URLs for security reasons (CORS policies).

You need to run a local web server to play the game. Here are the easiest ways to do it depending on what you have installed on your Mac:

### Option 1: Using Python (Recommended)
Macs come with Python pre-installed. This is the fastest way.
1. Open your terminal.
2. Navigate to your project folder:
   ```bash
   cd /Users/vaceslavefimov/projects/hackathons/tech-europe-2026
   ```
3. Run this command:
   ```bash
   python3 -m http.server 8000
   ```
4. Open your web browser and go to: `http://localhost:8000`

### Option 2: Using Node.js (npx)
If you have Node.js installed, you can use the `serve` package without installing it globally.
1. Open your terminal in the project folder.
2. Run this command:
   ```bash
   npx serve .
   ```
3. Open your web browser to the link it provides (usually `http://localhost:3000`).

### Option 3: VS Code "Live Server" Extension
If you are using Visual Studio Code as your editor:
1. Go to the Extensions tab and install **Live Server** (by Ritwick Dey).
2. Open `index.html` in VS Code.
3. Click the **"Go Live"** button in the bottom right corner of the window.
4. It will automatically open your default browser to the correct local address.
