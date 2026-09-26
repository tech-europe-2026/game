// BLACKICE TERMINAL — fake shell infiltration game
// Explore the filesystem, find the vault key, decrypt the flag.

const VAULT_KEY = "swordfish";
const FLAG = "FLAG{gr3p_4nd_y0u_sh4ll_f1nd}";

// ==========================================
// FAKE FILESYSTEM
// ==========================================
const file = (content) => ({ type: "file", content });
const dir = (children) => ({ type: "dir", children });

const FS = dir({
    "readme.txt": file(
`BLACKICE relay node — operator notes
=====================================
The payout flag lives in /vault/flag.enc (AES-encrypted).
Ops kept rotating the vault key and "temporarily" stashed it
in /etc — inside a dotfile, because "nobody ever looks there".

protip: dotfiles are hidden from 'ls'. Use 'ls -a' to see everything.
Useful commands: ls, cd, cat, pwd, decrypt. Type 'help' for the full list.`
    ),
    "docs": dir({
        "todo.txt": file(
`TODO (sysadmin):
  [x] rotate vault key — moved to /etc/.keys
  [x] purge old auth logs
  [ ] disable guest account (!!) — still open, do it monday`
        ),
        "motd": file(
`Unauthorized access to this system is prohibited.
All activity is monitored. Trace initiated on anomaly.`
        ),
    }),
    "etc": dir({
        ".keys": file(`VAULT_KEY=${VAULT_KEY}`),
        "passwd": file(
`root:x:0:0:root:/root:/bin/bash
daemon:x:1:1:daemon:/usr/sbin:/usr/sbin/nologin
guest:x:1000:1000:guest:/home/guest:/bin/terminal.js
sysop:x:1001:1001:sysop:/home/sysop:/bin/bash`
        ),
    }),
    "logs": dir({
        "auth.log": file(
`Mar 03 03:12:01 blackice sshd[412]: Failed password for root from 185.220.101.4
Mar 03 03:12:14 blackice sshd[412]: Failed password for admin from 185.220.101.4
Mar 03 03:12:31 blackice sshd[412]: Accepted password for guest from 10.13.37.9
Mar 03 03:13:02 blackice sudo[700]: sysop : vault key rotated -> /etc/.keys`
        ),
    }),
    "vault": dir({
        "flag.enc": file(
`-----BEGIN BLACKICE BLOCK-----
U2FsdGVkX19oZWxsIHdvcmxkIGl0IGlzIGVuY3J5cHRlZA==
bmljZSB0cnksIGJ1dCB0aGlzIGlzIGp1c3QgZmxhdm9yIHRleHQ=
-----END BLACKICE BLOCK-----

[ encrypted — requires: decrypt flag.enc <key> ]`
        ),
    }),
});

// ==========================================
// SHELL STATE
// ==========================================
let cwd = [];              // path segments from root
let traceCount = 0;        // commands run — the "trace" ticking clock
let history = [];
let histIdx = -1;
let won = false;

const output = document.getElementById("output");
const promptEl = document.getElementById("prompt");
const inputEl = document.getElementById("cmd");
const traceEl = document.getElementById("trace-counter");

// ==========================================
// FS HELPERS
// ==========================================
function resolve(path) {
    let parts = path.startsWith("/") || path.startsWith("~") ? [] : [...cwd];
    path = path.replace(/^~/, "");
    for (const seg of path.split("/")) {
        if (!seg || seg === ".") continue;
        if (seg === "..") parts.pop();
        else parts.push(seg);
    }
    return parts;
}

function nodeAt(parts) {
    let node = FS;
    for (const p of parts) {
        if (node.type !== "dir" || !(p in node.children)) return null;
        node = node.children[p];
    }
    return node;
}

function displayPath() {
    return cwd.length ? "/" + cwd.join("/") : "~";
}

// ==========================================
// OUTPUT
// ==========================================
function print(text, cls) {
    for (const line of String(text).split("\n")) {
        const div = document.createElement("div");
        div.className = "line" + (cls ? " " + cls : "");
        div.textContent = line;
        output.appendChild(div);
    }
    output.scrollTop = output.scrollHeight;
}

function printHtml(html) {
    const div = document.createElement("div");
    div.className = "line";
    div.innerHTML = html;
    output.appendChild(div);
    output.scrollTop = output.scrollHeight;
}

function setPrompt() {
    promptEl.textContent = `guest@blackice:${displayPath()}$`;
}

function bumpTrace() {
    traceCount++;
    traceEl.textContent = `TRACES: ${traceCount}`;
    if (traceCount === 15) print("[!] intrusion counter-measures warming up — keep it clean, operator", "warn");
    if (traceCount === 30) print("[!] trace at 30 commands. they're onto you. wrap it up.", "err");
}

// ==========================================
// COMMANDS
// ==========================================
const COMMANDS = {
    help() {
        print(
`available commands:
  ls [-a] [path]     list directory contents (-a shows hidden files)
  cd <dir>           change directory (.. goes up, / and ~ go home)
  pwd                print working directory
  cat <file>         print file contents
  decrypt <f> <key>  decrypt a file with a key
  nmap               scan the target (flavor)
  whoami             current user
  clear              clear the terminal
  exit               disconnect`);
    },

    pwd() {
        print(cwd.length ? "/" + cwd.join("/") : "/home/guest");
    },

    whoami() {
        print("guest — restricted shell. enough to get in, though.");
    },

    nmap() {
        print(
`Scanning 10.13.37.1 ...
PORT     STATE  SERVICE
22/tcp   open   ssh
443/tcp  open   https
31337/tcp open  blackice-vault   <— interesting`, "cyan");
        print("the vault answers on this host. hunt around the filesystem.", "dim");
    },

    sudo() {
        print("guest is not in the sudoers file. This incident will be reported.", "err");
        print("(nice try though)", "dim");
    },

    exit() {
        print("connection to 10.13.37.1 closed.", "dim");
        inputEl.disabled = true;
        setTimeout(() => { inputEl.disabled = false; inputEl.focus(); print("nah — you're in too deep to leave now.", "warn"); }, 1200);
    },

    clear() {
        output.innerHTML = "";
    },

    ls(args) {
        const showAll = args.includes("-a");
        const pathArg = args.find(a => !a.startsWith("-"));
        const node = pathArg ? nodeAt(resolve(pathArg)) : nodeAt(cwd);

        if (!node) return print(`ls: cannot access '${pathArg}': No such file or directory`, "err");
        if (node.type !== "dir") return print(pathArg);

        const names = Object.keys(node.children)
            .filter(n => showAll || !n.startsWith("."))
            .sort();

        if (!names.length) return print("(empty)", "dim");

        printHtml(names.map(n => {
            const child = node.children[n];
            const label = n + (child.type === "dir" ? "/" : "");
            return `<span class="${child.type === "dir" ? "dir" : ""}">${label}</span>`;
        }).join("&nbsp;&nbsp;"));
    },

    cd(args) {
        if (!args[0] || args[0] === "~") { cwd = []; setPrompt(); return; }
        const parts = resolve(args[0]);
        const node = nodeAt(parts);
        if (!node) return print(`cd: ${args[0]}: No such file or directory`, "err");
        if (node.type !== "dir") return print(`cd: ${args[0]}: Not a directory`, "err");
        cwd = parts;
        setPrompt();
    },

    cat(args) {
        if (!args[0]) return print("usage: cat <file>", "dim");
        const node = nodeAt(resolve(args[0]));
        if (!node) return print(`cat: ${args[0]}: No such file or directory`, "err");
        if (node.type === "dir") return print(`cat: ${args[0]}: Is a directory`, "err");
        print(node.content);
    },

    decrypt(args) {
        if (args.length < 2) return print("usage: decrypt <file> <key>", "dim");
        const [target, key] = args;
        const parts = resolve(target);
        const node = nodeAt(parts);
        if (!node) return print(`decrypt: ${target}: No such file or directory`, "err");
        if (parts[parts.length - 1] !== "flag.enc") {
            return print(`decrypt: ${target}: not an encrypted artifact`, "err");
        }
        print("decrypting ...", "dim");
        setTimeout(() => {
            if (key === VAULT_KEY) {
                print(`-----BEGIN DECRYPTED BLOCK-----\n${FLAG}\n-----END DECRYPTED BLOCK-----`, "ok");
                if (!won) {
                    won = true;
                    print(`\nACCESS GRANTED — flag captured in ${traceCount} commands.\nType 'exit' to disconnect, or keep poking around.`, "ok");
                }
            } else {
                print("DECRYPTION FAILED — wrong key", "err");
            }
        }, 400);
    },
};

// ==========================================
// INPUT LOOP
// ==========================================
function runCommand(raw) {
    const trimmed = raw.trim();
    printHtml(`<span class="cmd-echo">guest@blackice:${displayPath()}$ ${escapeHtml(raw)}</span>`);
    if (!trimmed) return;

    bumpTrace();
    const [cmd, ...args] = trimmed.split(/\s+/);
    if (COMMANDS[cmd]) COMMANDS[cmd](args);
    else print(`${cmd}: command not found — try 'help'`, "err");
}

function escapeHtml(s) {
    return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

inputEl.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
        runCommand(inputEl.value);
        if (inputEl.value.trim()) {
            history.push(inputEl.value);
            histIdx = history.length;
        }
        inputEl.value = "";
    } else if (e.key === "ArrowUp") {
        e.preventDefault();
        if (histIdx > 0) inputEl.value = history[--histIdx];
    } else if (e.key === "ArrowDown") {
        e.preventDefault();
        if (histIdx < history.length - 1) inputEl.value = history[++histIdx];
        else { histIdx = history.length; inputEl.value = ""; }
    }
});

document.getElementById("terminal").addEventListener("click", () => inputEl.focus());

// ==========================================
// BOOT SEQUENCE
// ==========================================
const BOOT = [
    ["[ ok ] uplink established → 10.13.37.1", "dim", 300],
    ["[ ok ] spoofed MAC 4a:3f:9c:xx:xx", "dim", 250],
    ["[ ok ] guest shell allocated — restricted mode", "dim", 250],
    ["BLACKICE relay node v2.4.1", "cyan", 400],
    ["type 'help' to list commands. 'readme.txt' is a good place to start.", "warn", 0],
];

let i = 0;
(function boot() {
    if (i < BOOT.length) {
        const [text, cls, delay] = BOOT[i++];
        print(text, cls);
        setTimeout(boot, delay);
    }
})();
setPrompt();
