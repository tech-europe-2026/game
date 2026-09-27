using System;
using System.Collections.Generic;
using UnityEngine;

public static class Levels
{
    public struct Level
    {
        public string name, unlock;
        public bool boss;
        public Color top, mid, bottom;
        public Vector2 camMin, camMax;
        public bool pit;
        public Action<LevelBuilder> build;
    }

    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    public static readonly Level[] All =
    {
        new Level { name = "FIRST FLIGHT", unlock = "jump,crouch,dash", build = FirstFlight, top = C(120, 170, 245), mid = C(186, 214, 255), bottom = C(255, 222, 214) },
        new Level { name = "UPSIDE", unlock = "jump,reverse,crouch", build = Upside, top = C(124, 106, 226), mid = C(198, 180, 255), bottom = C(255, 206, 226) },
        new Level { name = "HEAVY WEATHER", unlock = "jump,grow,dash", build = HeavyWeather, top = C(78, 170, 204), mid = C(172, 230, 236), bottom = C(238, 250, 244) },
        new Level { name = "PULSE", unlock = "jump,dash,crouch", build = PhaseShift, top = C(246, 134, 128), mid = C(255, 196, 170), bottom = C(255, 236, 204) },
        new Level { name = "DUEL", unlock = "jump,dash,grow", boss = true, camMin = V(-11, -4.7f), camMax = V(11, 6.3f), build = Duel, top = C(64, 60, 120), mid = C(170, 120, 190), bottom = C(255, 170, 160) },
        new Level { name = "SPRINGTIME", unlock = "jump,dash,slam,crouch", build = Springtime, top = C(255, 150, 190), mid = C(255, 204, 222), bottom = C(255, 240, 220) },
        new Level { name = "FLIPSIDE", unlock = "jump,reverse,hover,dash", build = Flipside, top = C(90, 110, 220), mid = C(150, 200, 250), bottom = C(220, 250, 240) },
        new Level { name = "GLASSWORKS", unlock = "jump,hover,grow,slam", build = Glassworks, top = C(70, 180, 190), mid = C(170, 225, 245), bottom = C(245, 235, 255) },
        new Level { name = "SKY RUSH", unlock = "jump,dash,hover,reverse", build = SkyRush, top = C(255, 140, 90), mid = C(255, 190, 150), bottom = C(200, 220, 255) },
        new Level { name = "RED STORM", unlock = "jump,dash,slam,hover", boss = true, camMin = V(-11, -4.7f), camMax = V(11, 6.3f), build = RedStorm, top = C(40, 30, 80), mid = C(120, 70, 150), bottom = C(255, 120, 140) },
        new Level { name = "BOUNCE HOUSE", unlock = "jump,crouch,slam,dash", build = BounceHouse, top = C(250, 110, 170), mid = C(255, 180, 200), bottom = C(255, 230, 190) },
        new Level { name = "BLIND DROP", unlock = "jump,crouch,parry,hover", build = BlindDrop, top = C(60, 80, 160), mid = C(130, 150, 220), bottom = C(220, 210, 250) },
        new Level { name = "GHOST LINE", unlock = "jump,phase,dash,reverse", build = GhostLine, top = C(110, 70, 190), mid = C(190, 150, 240), bottom = C(240, 220, 255) },
        new Level { name = "SKYFALL", unlock = "jump,hover,slam,parry", build = Skyfall, top = C(40, 130, 200), mid = C(140, 200, 240), bottom = C(255, 225, 200) },
        new Level { name = "CRIMSON RIFT", unlock = "jump,dash,slam,parry", boss = true, pit = true, camMin = V(-11, -4.7f), camMax = V(11, 6.3f), build = CrimsonRift, top = C(30, 15, 40), mid = C(110, 40, 80), bottom = C(255, 110, 90) },
        new Level { name = "SKY HIGHWAY", unlock = "jump,dash,slam,hover", build = SkyHighway, top = C(24, 36, 90), mid = C(96, 90, 200), bottom = C(255, 160, 130) },
    };

    // intro run that only a self-made gravity flip gets past: floor spikes, then (hard) ceiling spikes
    static void FlipGauntlet(LevelBuilder b, float x0, bool hard)
    {
        b.Start(x0 + 2, 1);
        b.Plat(x0 + 20.5f, 0, 41);
        b.Plat(x0 + 20.5f, 5, 35);
        b.Shards(x0 + 17, .35f, 21);
        b.Label(x0 + 5, 3, "spikes ahead · E flips you onto the ceiling|spikes ahead · REVERSE onto the ceiling");
        b.Orbs(x0 + 11, 4, x0 + 23, 4, 4);
        if (hard)
        {
            b.Shards(x0 + 32, 4.65f, 13, 180);
            b.Label(x0 + 30, 8, "now flip back down");
            b.Orbs(x0 + 28, 1.2f, x0 + 36, 1.2f, 3);
        }
        else b.Label(x0 + 34, 8, "flip back down before the ceiling ends");
        b.Check(x0 + 39, 1.4f);
    }

    // three green springs stepping up over the void, then a steep drop into the level
    static void SpringStairs(LevelBuilder b, float x0, bool hard)
    {
        b.Start(x0 + 2, 1);
        b.Plat(x0 + 4, 0, 8);
        b.Pad(x0 + 6.5f, .55f, 0, 16);
        b.Plat(x0 + 9.5f, 2.8f, 2.6f);
        b.Pad(x0 + 9.5f, 3.35f, 0, 16);
        b.Plat(x0 + 12.5f, 5.6f, 2.6f);
        b.Pad(x0 + 12.5f, 6.15f, 0, 16);
        b.Label(x0 + 4, 3.5f, "bounce up the springs");
        b.Orbs(x0 + 8, 5.5f, x0 + 14.5f, 11, 3);
        b.Plat(x0 + 21, 8.65f, 10);
        if (hard)
        {
            b.Blaster(x0 + 16, 13, V(-.4f, -1), 1.5f, 0, 25);
            b.Shards(x0 + 22, 9, 3);
        }
        b.Check(x0 + 18, 10.05f);
        b.Ramp(x0 + 26, 8.65f, x0 + 38, -.1f);
    }

    // Each finale strings three different rail shapes together; the style picks which three.
    static void RailRun(LevelBuilder b, float x, float py, int style = 0)
    {
        float y = py + .7f;
        Vector2 P(float dx, float dy) => V(x + dx, y + dy);
        switch (style % 4)
        {
            case 0: // one long roller: big dips and shrinking hills
                b.Rail(true, P(0, 0), P(4, -3), P(8, -4.4f), P(12, -2.3f), P(16, -4.9f), P(20, -3.3f), P(24.5f, -5.3f), P(29.2f, -4f));
                break;
            case 1: // rolling rail, hop, short downhill
                b.Rail(true, P(0, 0), P(5, -2.6f), P(10, -1.5f), P(15, -3.5f), P(18.5f, -2.9f));
                b.Rail(true, P(21.5f, -3.4f), P(25.5f, -4.4f), P(29.2f, -4f));
                break;
            case 2: // deep plunge that climbs out, then a flat run-in
                b.Rail(true, P(0, 0), P(6, -5f), P(12, -5.6f), P(18, -3.2f), P(21.5f, -2.8f));
                b.Rail(true, P(24.5f, -3.6f), P(29.2f, -4f));
                break;
            default: // three straight rails stepping down
                b.Rail(true, P(0, 0), P(7.2f, -.9f));
                b.Rail(true, P(10.7f, -1.9f), P(17.2f, -2.6f));
                b.Rail(true, P(20.7f, -3.4f), P(29.2f, -4f));
                break;
        }
        b.Orbs(x + 3, y - .6f, x + 22, y - 3.6f, 5);
        b.Plat(x + 33.2f, py - 3.6f, 8);
        b.Plat(x + 37.4f, py - 1.8f, .7f, 4f);
        b.Goal(x + 34.2f, py - 1.6f);
    }

    static void EndWall(LevelBuilder b, float x, float py)
    {
        b.Plat(x + .2f, py + 1.8f, .7f, 4f);
        b.Goal(x - 3, py + 2);
    }

    // rail hop onto a ledge, then a glass wave tube down to the goal
    static void FinaleTube(LevelBuilder b, float x, float py)
    {
        float y = py + .7f, c1 = py - 1.55f, c2 = c1 - 2f;
        b.Rail(true, V(x, y), V(x + 3.5f, y - 1.2f), V(x + 7.2f, y - .55f));
        b.Plat(x + 13.5f, c1, 7);
        b.Tube(V(x + 16.9f, c1 + 1.13f), V(x + 19.5f, c1 + 1.1f), V(x + 22, c1 - 1.6f), V(x + 24.5f, c1 - .3f), V(x + 27.1f, c1 - .87f));
        b.Orbs(x + 2, y - .4f, x + 14, c1 + 1.2f, 4);
        b.Plat(x + 31, c2, 8);
        EndWall(b, x + 35, c2);
    }

    // steep ramp into an orange kicker: only a fast ball clears the chasm
    static Vector2 SpeedKick(LevelBuilder b, float x, float py, bool goal)
    {
        float g = py - 4.5f;
        b.Ramp(x - .2f, py, x + 12, g);
        b.Plat(x + 14.5f, g, 5);
        b.Kicker(x + 14.6f, g + .45f, 2.4f, 26);
        b.Label(x + 6, py + 3, "orange kicker: arrive FAST · D for extra speed|arrive FAST · DASH for extra speed");
        b.Orbs(x + 18, g + 3, x + 24, g + 3, 3);
        b.Plat(x + 29, g, 8);
        if (goal) EndWall(b, x + 33, g);
        return V(x + 33, g);
    }

    // hop across islands on tilted green pads
    static Vector2 PadHops(LevelBuilder b, float x, float py, bool goal)
    {
        b.Plat(x + 3, py - 2, 4);
        b.Pad(x + 3.6f, py - 1.45f, -25, 21);
        b.Plat(x + 15, py - 1, 4);
        b.Pad(x + 15.6f, py - .45f, -25, 21);
        b.Orbs(x + 6, py + 4, x + 12, py + 4, 3);
        b.Orbs(x + 18, py + 5, x + 23, py + 5, 3);
        b.Plat(x + 29, py, 8);
        if (goal) EndWall(b, x + 33, py);
        return V(x + 33, py);
    }

    // one long rail dive with a loop of any shape, then the goal
    static void FinaleLoop(LevelBuilder b, float x, float py, float r, float ry, int turns, float shift)
    {
        float y = py + .7f, ly = y - 2.5f, xl = x + 9, xe = xl + LevelBuilder.LoopShiftFor(LevelBuilder.LoopWidth(r), shift) * turns + 5;
        b.Rail(true, V(x, y), V(x + 4, ly), V(xe, ly), V(xe + 3, ly + .8f));
        b.Loop(xl, ly, r, 1, ry, turns, shift);
        b.Orbs(x + 2, y - .8f, x + 6, ly + .6f, 3);
        b.Plat(xe + 7.5f, ly - .9f, 8);
        EndWall(b, xe + 11.5f, ly - .9f);
    }

    static void FirstFlight(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(3, 0, 12);
        b.Label(3, 3, "A / D to roll   ·   SPACE to jump|left thumb rolls   ·   tap JUMP");
        b.Orbs(5, 1.3f, 8, 1.3f, 3);
        b.Orb(10.5f, 2.6f);
        b.Plat(16, 0, 8);
        b.Ice(20, 0, 28, -3, 1);
        b.Label(24, 2.5f, "ice · you just slide");
        b.Plat(31, -3, 6);
        b.Check(31, -1.6f);

        b.Label(40, -.3f, "grind the rails");
        b.Rail(true, V(34.2f, -2.63f), V(37.5f, -3.8f), V(40.5f, -3.1f), V(43.5f, -4f), V(47.5f, -2.9f), V(51.8f, .3f));
        b.Orbs(38, -2.4f, 46, -1.7f, 4);

        b.Plat(56, 0, 8);
        b.Plat(58, 2.9f, 4, 1.1f);
        b.Label(53.5f, 2.6f, "hold S to shrink|hold CROUCH to shrink");
        b.Orb(58, .75f);
        b.Plat(66, 0, 8);

        b.Label(66, 2.6f, "mint pads launch you");
        b.Pad(68, .55f);
        b.Orbs(68, 3f, 68.5f, 6f, 3);
        b.Plat(78, 6, 18);
        b.Glass(78, 8.7f, .8f, 4f);
        b.Label(74, 11.4f, "SHIFT to dash through glass|DASH through glass");
        b.Spinner(84, 7.9f, 3.2f, 120);
        b.Orb(81, 7.3f);
        b.Check(86.2f, 7.4f);

        b.Label(92, 9.5f, "roll through the glass");
        b.Tube(V(86.4f, 7.13f), V(89.5f, 7.1f), V(92, 3.6f), V(94.5f, 5.1f), V(97.9f, 5.13f));
        b.Orbs(91.2f, 4.6f, 93.5f, 4.4f, 2);
        b.Plat(100.5f, 4, 6);
        b.Shards(100.5f, 4.35f, 3);
        b.Orb(100.5f, 6.4f);
        b.Label(110, 7, "ride the coaster");
        b.Coaster(V(103.8f, 4.35f), V(108, 1.5f), V(112, -2.5f), V(116, -1), V(120, 2.5f), V(124, .5f),
            V(128, -2), V(132, -.5f), V(136, 3), V(139.8f, 4.35f));
        b.Orbs(111, -1.4f, 113, -1.9f, 2);
        b.Orb(120, 3.6f);
        b.Orbs(127, -.9f, 129, -.9f, 2);
        b.Plat(145, 4, 10);
        b.Check(146, 5.4f);

        b.Label(155, 9, "blue lasers only push you back");
        b.Plat(153.75f, 4, 7.5f);
        b.Gate(155, 4.35f, 7.5f, 0, true);
        b.Tube(V(157.4f, 5.13f), V(160, 5.1f), V(163, 2.7f), V(166, 3.3f), V(168.6f, 4.63f));
        b.Orbs(162, 3.3f, 165, 3.4f, 2);
        b.Plat(172, 3.5f, 7);
        b.Ice(175.3f, 3.5f, 180, 2, 1);
        b.Plat(184, 2, 8);
        b.Check(182, 3.4f);
        b.Orb(186, 3.4f);
        b.Plat(191.5f, 3, 5);
        b.Orbs(190.5f, 4.5f, 192.5f, 4.5f, 2);
        b.Plat(198.5f, 2.5f, 5);
        b.Gate(198.5f, 2.85f, 6, -.8f, true);
        b.Plat(205, 2.5f, 8);
        RailRun(b, 209.3f, 2.5f);
    }

    static void Upside(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(3, 0, 10);
        b.Label(2, 3, "E flips gravity|REVERSE flips gravity");
        b.Plat(15, 6, 20);
        b.Shards(15, 5.65f, 3, 180);
        b.Orb(10, 4.6f);
        b.Orb(12.5f, 4.6f);
        b.Orb(17.5f, 4.6f);
        b.Orb(20, 4.6f);
        b.Label(28, 3.5f, "flip back to land");
        b.Plat(32, 1, 10);

        b.Pad(34.5f, 1.55f, 0, 16);
        b.Plat(37.5f, 3.8f, 2.6f);
        b.Pad(37.5f, 4.35f, 0, 16);
        b.Plat(40.5f, 6.6f, 2.6f);
        b.Pad(40.5f, 7.15f, 0, 16);
        b.Label(33, 6, "bounce up the steps");
        b.Orbs(36, 5.5f, 42.5f, 11, 3);
        b.Plat(49, 9.65f, 10);
        b.Check(50, 11.05f);
        b.Orbs(42, 11, 46, 11, 3);

        b.Label(58, 12.2f, "flip · roll · flip");
        b.Plat(64, 15, 14);
        b.Spinner(64, 13.2f, 3, -140);
        b.Orb(60, 13.9f);
        b.Orb(68, 13.9f);
        b.Plat(78.5f, 10, 9);
        b.Check(80, 11.2f);

        b.Label(92, 13.5f, "jump rail to rail · don't drop");
        b.Rail(true, V(83.3f, 10.35f), V(87, 9.2f), V(90.5f, 9.8f));
        b.Orbs(86, 10.4f, 89, 10.6f, 2);
        b.Rail(true, V(94, 8.6f), V(97.5f, 7.3f), V(100.5f, 8.1f));
        b.Orb(97.5f, 8.5f);
        b.Rail(true, V(104, 7.2f), V(107.5f, 5.6f), V(112.5f, 6.35f));
        b.Orbs(106, 6.9f, 110, 6.7f, 2);
        b.Ice(112.8f, 6, 118, 6, 1);
        b.Plat(121, 6, 6);
        b.Check(121, 7.4f);

        b.Plat(128, 6, 8);
        b.Plat(129, 9, 5, 3.7f);
        b.Label(124, 8.5f, "hold S to squeeze|hold CROUCH to squeeze");
        b.Orbs(127.5f, 6.8f, 130.5f, 6.8f, 3);
        b.Label(137, 9, "flip over the gap");
        b.Plat(137, 11, 12);
        b.Orbs(135, 10, 141, 10, 3);
        b.Plat(148, 6, 8);
        b.Gate(148, 6.35f, 9.5f, 0, true);
        b.Plat(154, 9, 1.2f, 8);
        b.Pad(150.8f, 6.55f, 0, 22);
        b.Label(150, 11, "bounce over");
        b.Plat(159, 12.65f, 9);
        b.Check(161.5f, 14.05f);
        b.Orb(159, 14);
        b.Rail(true, V(163.3f, 13.35f), V(168, 12), V(173, 12.8f));
        b.Orbs(166, 12.8f, 170, 12.8f, 2);
        b.Plat(177, 12.5f, 7);
        FinaleTube(b, 180.8f, 12.5f);
    }

    static void HeavyWeather(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(4, 0, 12);
        b.Label(4, 3, "G to grow heavy|GROW to get heavy");
        b.Seesaw(16, 1.2f, 8);
        b.Box(19, 2.6f);
        b.Orbs(13, 3, 19, 3, 3);
        b.Plat(26, 1, 8);
        b.Check(26, 2.4f);
        b.Glass(33, 1, 6, .7f);
        b.Plat(37, 4, 2, 8);
        b.Label(29, 3.8f, "jump grown to smash glass");
        b.Orb(33, 2.5f);

        b.Plat(46.5f, -5, 27);
        b.Plat(47, .5f, 20, 1);
        b.Label(44, -1.4f, "grow heavy to shove the crates");
        b.Orb(40, -3.8f);
        b.Box(46, -3.8f);
        b.Box(51, -3.8f);
        b.Orbs(48, -3.8f, 54, -3.8f, 3);
        b.Pad(58.5f, -4.45f, 0, 24);
        b.Plat(64, 3, 8);
        b.Check(62, 4.5f);

        b.Tube(V(67.6f, 4.13f), V(70.5f, 1.4f), V(73.5f, 1.4f), V(76.4f, 4.13f));
        b.Plat(80, 3, 8);
        b.Turret(76, 9.5f, Vector2.down, 1.5f, .7f);
        b.Orbs(71, 1.5f, 73, 1.5f, 2);
        b.Orbs(78, 4.5f, 82, 4.5f, 2);
        b.Label(92, 6.5f, "ice · no steering, just slide");
        b.Ice(84.3f, 3, 97, -2, 1);
        b.Ice(97, -2, 104, -2, 1);
        b.Orbs(99, -1.1f, 103, -1.1f, 3);
        b.Coaster(V(105.5f, -2.6f), V(110, -6), V(115, -3), V(119, 0), V(123, -2.5f), V(127, -4), V(131, -1), V(134.8f, .35f));
        b.Orb(119, 1.1f);
        b.Plat(140, 0, 10);
        b.Check(140, 1.4f);

        b.Plat(150, 0, 10);
        b.Box(149, .95f);
        b.Box(152, .95f);
        b.Tube(V(154.6f, 1.13f), V(158, 1.1f), V(161, -1.5f), V(164, -1.5f), V(167.4f, .13f));
        b.Orbs(160, -1.2f, 164, -1.2f, 3);
        b.Plat(171, -1, 8);
        b.Check(170, .4f);
        b.Ice(175, -1, 181, -3, 1);
        b.Plat(185, -3, 8);
        b.Gate(186, -2.65f, 1, .5f, true);
        b.Orb(183, -1.6f);
        b.Plat(193, -3, 8);
        RailRun(b, 197.3f, -3f, 2);
    }

    static void PhaseShift(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(4, 0, 12);
        b.Label(6, 3, "red lasers pulse · wait for the gap");
        b.Plat(22, 0, 20);
        b.Check(15, 1.4f);
        b.Orb(15.5f, 1.4f);

        b.Plat(26, 5, 10, 1.2f);
        b.Gate(24, .35f, 4.4f);
        b.Gate(28, .35f, 4.4f, -.5f);
        b.Orbs(23, 1.3f, 29, 1.3f, 3);

        b.Gate(33.5f, -3, 7, -1f);
        b.Plat(38, 0, 6);
        b.Mover(44, 1, 3, 0, 5, 4);
        b.Orbs(44, 3, 44, 7, 3);
        b.Plat(50, 6.5f, 6);

        b.Rail(true, V(53.3f, 6.83f), V(56, 5.2f), V(59, 6), V(62, 4.8f), V(66, 5.6f), V(69.8f, 8.3f));
        b.Gate(61, 5.8f, 9.5f, -.7f);
        b.Orbs(56, 6.2f, 66, 6.8f, 4);
        b.Label(58, 10.5f, "time the rail");
        b.Plat(75, 8, 10);
        b.Check(73, 9.4f);

        b.Gate(79, 8.35f, 12, -1.2f);
        b.Plat(84, 8, 8);
        b.Orb(82, 9.4f);
        b.Plat(95, 14, 12);
        b.Shards(95, 13.4f, 2, 180);
        b.Tube(V(87.6f, 9.13f), V(91, 6.6f), V(95, 6.2f), V(99, 7.6f), V(103.2f, 8.98f));
        b.Orbs(91, 6.7f, 99, 7.7f, 3);
        b.Ice(103, 7.9f, 106.5f, 7.9f, 1);
        b.Plat(109, 8, 5);
        b.Check(109, 9.4f);

        b.Plat(115.25f, 8, 7.5f);
        b.Gate(116, 8.35f, 11, -.3f);
        b.Plat(122.5f, 9.3f, 5);
        b.Orb(122, 10.7f);
        b.Plat(129, 10.8f, 6);
        b.Gate(128, 11.15f, 14.3f, 0, true);
        b.Rail(true, V(132.3f, 11.5f), V(137, 9.8f), V(142, 10.6f));
        b.Orbs(135, 10.4f, 139, 10.4f, 2);
        b.Plat(146, 10.3f, 8);
        b.Check(145, 11.7f);
        PadHops(b, 150.3f, 10.3f, true);
    }

    static void Duel(LevelBuilder b)
    {
        b.Start(-7, -2.5f);
        b.Plat(0, -4, 20);
        b.Plat(-10.4f, .6f, .8f, 10);
        b.Plat(10.4f, .6f, .8f, 10);
        b.Plat(0, 6, 21.6f, .6f);
        b.Plat(-5.5f, -1.7f, 4);
        b.Plat(5.5f, -1.7f, 4);
        b.Plat(0, .6f, 4);
        b.Label(0, 3.2f, "ram the red ball · dash hits hardest");
        b.Rival(6, -2.5f, new Vector2(-10, -3.65f), new Vector2(10, 5.5f));
    }

    // SPRINGTIME: trampolines, slam through glass, a first loop.
    static void Springtime(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(3, 0, 12);
        b.Label(4, 3, "pink springs bounce · hold SPACE for more|hold JUMP on springs to fly higher");
        b.Tramp(11, -1.5f, 4);
        b.Orbs(11, 1, 11, 4, 3);
        b.Plat(18, 3, 8);
        b.Check(18, 4.4f);
        b.Plat(28, 3, 8);
        b.Plat(28, 5.15f, 6, 2);
        b.Label(28, 7.6f, "S to squeeze|hold CROUCH to squeeze");
        b.Orbs(26, 3.8f, 30, 3.8f, 3);
        b.Plat(35.5f, 3, 3);
        b.Glass(39, 3, 4, .7f);
        b.Plat(42, 6, 1, 6);
        b.Label(38, 5.8f, "jump + F slams through glass|jump + SLAM through glass");
        b.Plat(44, -1, 12);
        b.Check(45, .4f);
        b.Tramp(52, -1.2f, 2.4f);
        b.Plat(57, 2.3f, 5);
        b.Check(57, 3.7f);
        b.Rail(true, V(59.8f, 3f), V(64, .6f), V(68, .3f), V(78, .3f), V(82, .9f));
        b.Loop(72, .3f, 2.2f);
        b.Label(66, 4.6f, "loop the loop!");
        b.Plat(86, .6f, 6);
        b.Check(86, 2);
        b.Tramp(92.5f, .3f, 2.6f);
        b.Plat(97, 3, 3);
        b.Tramp(101, 2.4f, 2.6f);
        b.Plat(105.5f, 5.2f, 3);
        b.Tramp(109.5f, 4.6f, 2.6f);
        b.Orbs(97, 5, 109, 9, 4);
        b.Plat(115, 7.4f, 8);
        b.Check(115, 8.8f);
        b.Tube(V(119, 8.53f), V(122, 8.4f), V(125, 6.2f), V(128, 5.8f), V(131, 3.6f), V(134.2f, 3.53f));
        b.Plat(138, 2.4f, 8);
        b.Check(137, 3.8f);
        b.Plat(148, 2.4f, 12);
        b.Plat(147, 6.4f, 10);
        b.Blaster(150, 4.35f, V(-1, -.7f), 1.4f);
        b.Blaster(144.5f, 5.45f, V(-1, -1), 1.7f, .7f);
        b.Label(143, 8.6f, "blue shots ricochet and knock you back · S to slip under|CROUCH to slip under the blue blaster");
        b.Check(155, 3.8f);
        SpeedKick(b, 154.3f, 2.4f, true);
    }

    // FLIPSIDE: purple fields flip gravity, hover past a laser, a loop, ceiling runs.
    static void Flipside(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(3, 0, 12);
        b.Label(4, 3, "purple fields flip gravity");
        b.Plat(14, 0, 6);
        b.GravZone(20, 3, 4, 8, -1);
        b.Plat(26, 7.5f, 14);
        b.Orbs(22, 6.4f, 30, 6.4f, 4);
        b.GravZone(35, 3.5f, 3, 8, 1);
        b.Plat(40, 0, 8);
        b.Check(40, 1.4f);
        b.Gate(47, -1, 4);
        b.Label(47, 5, "jump, then Q to hover till the laser drops|jump, then HOVER till the laser drops");
        b.Plat(52, 0, 4);
        b.Plat(60, 0, 8);
        b.Glass(61, 1.85f, .8f, 3);
        b.Label(58, 4.5f, "D dashes through glass|DASH through glass");
        b.Plat(67, 0, 6);
        b.Check(67, 1.4f);
        b.Rail(true, V(70.3f, .7f), V(74, -1.5f), V(78, -2), V(88, -2), V(92, -1.2f));
        b.Loop(82, -2, 2.2f);
        b.Plat(96, -1.5f, 6);
        b.Plat(106, -1.5f, 12);
        b.Shards(106, -1.15f, 5);
        b.Plat(106, 3.2f, 14);
        b.Label(104, 5.4f, "E runs on the ceiling|REVERSE to run on the ceiling");
        b.Orbs(102, 2.2f, 110, 2.2f, 3);
        b.Plat(118, -1.5f, 8);
        b.Check(118, -.1f);
        b.GravZone(124, 1.5f, 3, 9, -1);
        b.Plat(132, 5.5f, 12);
        b.Orbs(128, 4.4f, 136, 4.4f, 3);
        b.GravZone(140, 1.5f, 3, 9, 1);
        b.Plat(146, -1, 8);
        b.Check(146, .4f);
        b.Plat(155, -1, 10);
        b.Frost(154, -.65f, 3);
        b.Label(154, 5, "cyan frost: 3 s of drifting, no control");
        b.Plat(162.5f, -4, 3);
        b.Pad(162.5f, -3.45f, 0, 20);
        b.Plat(174, 2, 16);
        b.Check(168, 3.4f);
        b.Blaster(174, 6, V(-.4f, -1), 1.1f, 0, 35);
        b.Label(174, 8.4f, "the blaster swivels · dash past");
        b.Tube(V(181.9f, 3.13f), V(185, 3.1f), V(188, 5.8f), V(191, 6.2f), V(194, 4.2f), V(197.2f, 3.13f));
        b.Plat(201, 2, 8);
        b.Check(200, 3.4f);
        FinaleLoop(b, 205.3f, 2, 1.7f, 3.2f, 1, 1.2f);
    }

    // GLASSWORKS: slam through a glass floor, hover a wide gap, spring-slam launch.
    static void Glassworks(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(3, 0, 12);
        b.Label(4, 3, "G grows heavy · F slams down|GROW heavy · SLAM down");
        b.Plat(15, 0, 10);
        b.Box(14, 1);
        b.Box(16.5f, 1);
        b.Glass(22.5f, 0, 5, .7f);
        b.Plat(27.5f, 0, 5);
        b.Plat(30.5f, 4, 1, 8);
        b.Label(23, 2.5f, "break the glass floor");
        b.Plat(28, -4, 14);
        b.Plat(40, -4, 10);
        b.Check(38, -2.6f);
        b.Label(49, -.5f, "jump + Q hovers across|jump + HOVER across");
        b.Orbs(46, -2.5f, 52, -2.5f, 3);
        b.Plat(55.5f, -4, 4);
        b.Tramp(61, -4.2f, 4);
        b.Label(61, -1, "hold SPACE on springs|hold JUMP on springs");
        b.Plat(67, -.5f, 8);
        b.Check(67, .9f);
        b.Ice(71.2f, -.5f, 78, -3, 1);
        b.Rail(true, V(78.3f, -2.6f), V(82, -3.2f), V(92, -3.2f), V(95, -2.6f));
        b.Loop(87, -3.2f, 2f);
        b.Plat(99, -3, 6);
        b.Check(99, -1.6f);
        b.Tramp(105, -3.2f, 3);
        b.Label(105, 1, "jump + F on the spring = launch|SLAM the spring to launch");
        b.Orbs(106, 0, 109, 5, 3);
        b.Plat(112, 4.5f, 8);
        b.Check(112, 5.9f);
        b.Plat(122, 4.5f, 10);
        b.Box(120, 5.5f);
        b.Box(122, 5.5f);
        b.Box(124, 5.5f);
        b.Label(122, 8, "grow to shove crates");
        b.Tube(V(126.9f, 5.63f), V(129, 5.6f), V(131, 4), V(131.6f, 0), V(132.6f, -2.5f), V(135, -3.37f), V(138.2f, -3.37f));
        b.Plat(142, -4.5f, 8);
        b.Check(141, -3.1f);
        b.Blaster(150, -.5f, V(-1, -1), 1.3f);
        b.Label(150, 2.5f, "hover across · blue shots bounce off everything");
        b.Plat(157, -4.5f, 8);
        b.Check(155, -3.1f);
        b.Frost(158, -4.15f, -.5f);
        var e = PadHops(b, 161.3f, -4.5f, false);
        RailRun(b, e.x + .3f, e.y, 5);
    }

    // SKY RUSH: fast rails, double loop, gravity tunnel, laser hover.
    static void SkyRush(LevelBuilder b)
    {
        FlipGauntlet(b, -44, false);
        b.Plat(3, 0, 12);
        b.Label(4, 3, "full speed ahead");
        b.Rail(true, V(9.3f, .7f), V(14, -2), V(18, -2.8f), V(30, -2.8f), V(34, -2));
        b.Loop(24, -2.8f, 2.4f);
        b.Rail(true, V(37.5f, -3.2f), V(41, -4.5f), V(45, -3.8f));
        b.Plat(49, -4, 6);
        b.Check(49, -2.6f);
        b.GravZone(54, 0, 3, 10, -1);
        b.Plat(62, 4, 14);
        b.Orbs(57, 2.9f, 67, 2.9f, 4);
        b.GravZone(71, 0, 3, 10, 1);
        b.Plat(77, -2, 8);
        b.Glass(79.5f, -.15f, .8f, 3);
        b.Label(76, 2.5f, "D smashes glass|DASH through glass");
        b.Check(75, -.6f);
        b.Gate(84.5f, -1.5f, 3.5f);
        b.Label(84.5f, 5, "hover till the laser drops");
        b.Plat(90, -2, 4);
        b.Check(90, -.6f);
        b.Plat(98, -2, 8);
        b.Shards(98, -1.65f, 6);
        b.Plat(98, 2.2f, 12);
        b.Label(98, 4.4f, "flip past the spikes");
        b.Plat(108, -2, 8);
        b.Rail(true, V(112.3f, -1.3f), V(116, -3.5f), V(120, -4), V(136, -4), V(140, -3.2f));
        b.Loop(124, -4, 2.2f);
        b.Loop(132, -4, 2.2f);
        b.Label(128, 1.5f, "double loop!");
        b.Plat(144, -3.5f, 6);
        b.Check(144, -2.1f);
        b.Tube(V(146.9f, -2.37f), V(150, -2.4f), V(152.5f, -4), V(155, -2.4f), V(157.5f, -4), V(160, -2.4f), V(162.5f, -3.9f), V(165.2f, -3.37f));
        b.Plat(169, -4.5f, 8);
        b.Frost(170, -4.15f, -.5f);
        b.Label(170, -1.5f, "wait for the frost to fade, then jump");
        b.Plat(183, -4.5f, 12);
        b.Check(179, -3.1f);
        b.Blaster(186, 0, V(-1, -.6f), .9f, 0, 50);
        b.Plat(191.5f, -4.5f, 5);
        var e = SpeedKick(b, 194.3f, -4.5f, false);
        b.Check(e.x - 6, e.y + 1.4f);
        FinaleLoop(b, e.x + .3f, e.y, 1.8f, 1.8f, 2, 3.2f);
    }

    // RED STORM: second boss; springs, a U-rail and side ledges; homing orbs, bullet rings, quake shockwaves.
    static void RedStorm(LevelBuilder b)
    {
        b.Start(-4, -2.5f);
        b.Plat(0, -4, 20);
        b.Plat(-10.4f, .6f, .8f, 10);
        b.Plat(10.4f, .6f, .8f, 10);
        b.Plat(0, 6, 21.6f, .6f);
        b.Tramp(-7.5f, -3.5f, 2.2f);
        b.Tramp(7.5f, -3.5f, 2.2f);
        b.Mover(-8, 1.6f, 2.4f, 0, 1.6f, 4.5f);
        b.Mover(8, 1.6f, 2.4f, 0, 1.6f, 4.5f, .5f);
        b.Rail(true, V(-5.2f, 2.2f), V(0, .4f), V(5.2f, 2.2f));
        b.Label(0, 4.3f, "slam it from above · jump the shockwaves");
        b.Rival(4, -2.5f, new Vector2(-10, -3.65f), new Vector2(10, 5.5f), 1);
    }

    // springs over the void; spikes above punish bouncing too high (S = soft, F = super)
    static Vector2 TrampChain(LevelBuilder b, float x, float py, int variant)
    {
        var pads = variant == 0
            ? new[] { V(4, -3), V(10, -2), V(16, -3.5f), V(22, -1.5f) }
            : new[] { V(4, -2.5f), V(9.5f, -4), V(15, -1.5f), V(20.5f, -3), V(25.5f, -2) };
        for (int i = 0; i < pads.Length; i++)
        {
            b.Tramp(x + pads[i].x, py + pads[i].y, 2.4f);
            if ((variant == 0 && (i == 1 || i == 2)) || (variant == 1 && (i == 0 || i == 3)))
            {
                float cy = py + pads[i].y + 5.6f;
                b.Plat(x + pads[i].x, cy + .2f, 3, .4f);
                b.Shards(x + pads[i].x, cy, 4, 180);
            }
            b.Orb(x + pads[i].x, py + pads[i].y + 3);
        }
        float end = x + pads[pads.Length - 1].x + 7;
        b.Plat(end, py, 6);
        b.Check(end, py + 1.4f);
        return V(end + 3, py);
    }

    // three glass squeeze lanes drop through two crossing chambers; only one exit is safe
    // three squeeze tubes braid all the way down; at every X crossing the walls open so you can switch lanes.
    // lane 3 drops onto the platform, lanes 1-2 into the pit unless you catch the drifting ledge
    static Vector2 Maze(LevelBuilder b, float x0, float top, int safe)
    {
        float[] lx = { 2, 7, 12 };
        int[] swaps = { 1, 0, 1, 0, 1, 0 };
        float step = 5f, bot = top - step * swaps.Length, R = .45f;
        int[] lane = { 0, 1, 2 };
        var paths = new List<Vector2>[3];
        for (int t = 0; t < 3; t++) paths[t] = new List<Vector2> { V(x0 + lx[t], top), V(x0 + lx[t], top - 1.5f), V(x0 + lx[t], top - 3) };
        for (int k = 0; k < swaps.Length; k++)
        {
            int a = swaps[k], c = a + 1;
            float yn = top - 3 - (k + 1) * (top - 3 - bot) / swaps.Length;
            for (int t = 0; t < 3; t++)
            {
                if (lane[t] == a) lane[t] = c;
                else if (lane[t] == c) lane[t] = a;
                paths[t].Add(V(x0 + lx[lane[t]], yn));
            }
        }
        for (int t = 0; t < 3; t++) paths[t].Add(V(x0 + lx[lane[t]], bot - 1.5f));
        for (int t = 0; t < 3; t++)
        {
            var others = new List<Vector2[]>();
            for (int o = 0; o < 3; o++) if (o != t) others.Add(paths[o].ToArray());
            b.TubeX(R, others.ToArray(), paths[t].ToArray());
            b.Label(x0 + lx[t], top + 1.3f, (t + 1).ToString());
        }
        b.Mover(x0 + 5.5f, bot - 9.5f, 2.4f, 3.5f, 0, 4f);
        float yc = top - .35f;
        b.Plat(x0 - 1.25f, yc, 5.5f);
        b.Plat(x0 + 4.5f, yc, 4f);
        b.Plat(x0 + 9.5f, yc, 4f);
        b.Plat(x0 + 13.3f, yc, 1.6f);
        b.Plat(x0 + 14.4f, yc + 2, .7f, 4.6f);
        b.Cover(x0 + 7, (top + bot) / 2, 14.6f, top - bot + 3, "one lane is safe · S to squeeze · switch lanes at the crossings|one lane is safe · CROUCH to squeeze · switch lanes at the crossings");
        b.Plat(x0 + 18.25f, bot - 7, 13.5f);
        b.Check(x0 + 19, bot - 5.6f);
        Ball.squeezeZone = Rect.MinMaxRect(x0 - .6f, bot - 1.6f, x0 + 14.6f, top - .4f);
        b.Fog(V(x0 - 1, bot - 12), V(x0 + 25.5f, top - 3), V(x0 - .6f, bot - 16), V(x0 + 26, top + .2f));
        return V(x0 + 25, bot - 7);
    }

    // BOUNCE HOUSE: trampoline chains with spike ceilings, blaster crossfire, frost drop.
    static void BounceHouse(LevelBuilder b)
    {
        SpringStairs(b, -41, false);
        b.Plat(3, 0, 12);
        b.Label(4, 3, "springs! S = soft bounce · F = super bounce|CROUCH = soft bounce · SLAM = super bounce");
        var e = TrampChain(b, 9.3f, 0, 0);
        b.Plat(e.x + 8, e.y, 16);
        b.Plat(e.x + 8, e.y + 6.8f, 16);
        b.Blaster(e.x + 5, e.y + 4.6f, V(-1, -1), 1.2f);
        b.Blaster(e.x + 9, e.y + 5.2f, V(-.3f, -1), 1.5f, .5f);
        b.Blaster(e.x + 13, e.y + 4.6f, V(-1, -.6f), 1f, .8f, 25);
        b.Label(e.x + 8, e.y + 9, "crossfire · D dashes through|crossfire · DASH through");
        var k = SpeedKick(b, e.x + 16.3f, e.y, false);
        b.Plat(k.x + 5, k.y, 10);
        b.Frost(k.x + 5, k.y + .35f, k.y + 4);
        b.Tramp(k.x + 16, k.y - 1.5f, 3);
        b.Plat(k.x + 23, k.y + 1.5f, 8);
        b.Check(k.x + 22, k.y + 2.9f);
        var t = TrampChain(b, k.x + 27.3f, k.y + 1.5f, 1);
        FinaleLoop(b, t.x + .3f, t.y, 2.6f, 1.4f, 1, 2.2f);
    }

    // BLIND DROP: shield the blue shots, frost over a hover gap, a hidden three-tube maze.
    static void BlindDrop(LevelBuilder b)
    {
        SpringStairs(b, -41, true);
        b.Plat(3, 0, 12);
        b.Label(4, 3, "J = shield · reflects shots|SHIELD reflects shots");
        b.Plat(17, 0, 16);
        b.Blaster(14, 5, V(-.3f, -1), 1.3f);
        b.Blaster(19, 5.5f, V(-.6f, -1), 1.1f, .4f);
        b.Blaster(24, 5, V(-1, -.8f), 1.5f, .8f, 20);
        b.Plat(33, 0, 6);
        b.Check(32, 1.4f);
        b.Frost(33.5f, .35f, 4);
        b.Label(39.5f, 4, "jump + Q hovers · frost fades every 3 s|jump + HOVER · frost fades every 3 s");
        b.Plat(47, 0, 8);
        b.Check(46, 1.4f);
        var m = Maze(b, 55, .35f, 2);
        b.Tube(V(m.x - .1f, m.y + 1.13f), V(m.x + 3, m.y + 1.1f), V(m.x + 5, m.y + 3), V(m.x + 7.5f, m.y + .2f), V(m.x + 10, m.y - 2), V(m.x + 13.2f, m.y - .87f));
        b.Plat(m.x + 17, m.y - 2, 8);
        b.Blaster(m.x + 17, m.y + 3.5f, V(-1, -1), 1.2f, 0, 40);
        b.Check(m.x + 15, m.y - .6f);
        RailRun(b, m.x + 21.3f, m.y - 2, 3);
    }

    // GHOST LINE: phase through purple walls, flip onto ceilings, mixed loop shapes.
    static void GhostLine(LevelBuilder b)
    {
        FlipGauntlet(b, -44, true);
        b.Plat(3, 0, 12);
        b.Plat(15, 0, 14);
        b.PhaseWall(16, 2.35f, .8f, 4);
        b.Label(12, 4.5f, "V phases through purple walls|PHASE through purple walls");
        b.GravZone(25, 3.5f, 3, 9, -1);
        b.Plat(33, 7.5f, 12);
        b.PhaseWall(35, 5.65f, .8f, 3);
        b.Orbs(29, 6.4f, 38, 6.4f, 4);
        b.GravZone(42, 3.5f, 3, 9, 1);
        b.Plat(47, 0, 8);
        b.Check(46, 1.4f);
        b.Plat(60, 0, 14);
        b.Frost(57, .35f, 4);
        b.Blaster(62, 5, V(-1, -1), 1f);
        b.Blaster(65, 4.5f, V(-1, -.5f), 1.3f, .6f);
        b.PhaseWall(64, 2.35f, .8f, 4);
        b.Check(55, 1.4f);
        b.Rail(true, V(67.3f, .7f), V(71, -1.8f), V(95, -1.8f), V(98, -1));
        b.Loop(76, -1.8f, 1.6f, 1, 2.6f, 1, 1.2f);
        b.Loop(85, -1.8f, 2.6f, 1, 1.5f, 1, 2f);
        b.Plat(102, -1.5f, 8);
        b.Check(101, -.1f);
        b.Tube(V(105.9f, -.37f), V(109, -.4f), V(111, 1.5f), V(113, -.4f), V(116, -2.4f), V(119.2f, -2.37f));
        b.Plat(123, -3.5f, 8);
        b.PhaseWall(124, -1.15f, .8f, 4);
        SpeedKick(b, 127.3f, -3.5f, true);
    }

    // SKYFALL: slam-launch, down-firing blasters, mid-air frost, a vertical S tube, spring chain.
    static void Skyfall(LevelBuilder b)
    {
        SpringStairs(b, -41, true);
        b.Plat(3, 0, 12);
        b.Label(4, 3, "jump + F on the spring to launch high|SLAM the spring to launch high");
        b.Tramp(11.5f, -1.5f, 2.6f);
        b.Plat(18, 7, 6);
        b.Check(18, 8.4f);
        b.Plat(29, 7, 12);
        b.Blaster(26, 11, V(0, -1), 1.2f);
        b.Blaster(30, 11.5f, V(0, -1), 1.2f, .4f);
        b.Blaster(34, 11, V(-.5f, -1), 1f, .8f, 30);
        b.Label(30, 13.5f, "J shields you from the blue rain|SHIELD from the blue rain");
        b.Frost(38.5f, 6.5f, 11);
        b.Label(38.5f, 13, "hover through when the frost is gone");
        b.Plat(46, 7, 8);
        b.Check(45, 8.4f);
        b.Tube(V(49.9f, 8.13f), V(53, 8.1f), V(55, 5), V(53, 2), V(55, -1), V(58.2f, -1.87f));
        b.Plat(62, -3, 8);
        b.Check(61, -1.6f);
        var t = TrampChain(b, 66.3f, -3, 1);
        b.Plat(t.x + 6, t.y, 6);
        b.Blaster(t.x + 6, t.y + 4.5f, V(-1, -1), 1.1f, 0, 35);
        b.Blaster(t.x + 9, t.y + 4, V(-1, -.4f), 1.4f, .5f);
        RailRun(b, t.x + 9.3f, t.y, 1);
    }

    // SKY HIGHWAY: tiny spring islands over pits, dropping rails you must leave on time, long-range trampolines.
    static void SkyHighway(LevelBuilder b)
    {
        b.Start(-2, 1);
        b.Plat(3, 0, 10);
        b.Label(3, 3.5f, "no floor ahead · every landing counts");
        float[] hy = { -2, -1, 0 };
        for (int i = 0; i < 3; i++)
        {
            float px = 11 + i * 12;
            b.Plat(px, hy[i], 3);
            b.Pad(px + .6f, hy[i] + .55f, -25, 21);
            b.Orbs(px + 4, hy[i] + 5, px + 8, hy[i] + 5, 2);
        }
        b.Plat(48, 1, 6);
        b.Check(47, 2.4f);

        b.Label(57, 5, "rails end in the air · jump at the lip");
        b.Rail(true, V(51.3f, 1.7f), V(56, -1), V(60, -3), V(63.5f, -2.2f));
        b.Rail(true, V(67.5f, -3.3f), V(73, -5.5f), V(79, -6.2f), V(83, -5.2f));
        b.Orbs(58, -1, 80, -4.4f, 5);
        b.Blaster(75, -1, V(-1, -1), 1.6f, 0, 25);
        b.Plat(89, -7, 6);
        b.Check(88, -5.6f);

        var t = TrampChain(b, 92.3f, -7, 0);
        float sx = t.x + 2;
        for (int i = 0; i < 3; i++)
        {
            b.Plat(sx + i * 3.4f, t.y + i * 2.8f, 2.2f);
            b.Pad(sx + i * 3.4f, t.y + i * 2.8f + .55f, 0, 16);
        }
        float hx = sx + 12, hyy = t.y + 8.4f;
        b.Plat(hx, hyy, 6);
        b.Check(hx - 1, hyy + 1.4f);
        b.Shards(hx + 1.5f, hyy + .35f, 2);

        b.Label(hx + 10, hyy + 3, "the lower rail drops into the pit · jump to the upper one");
        b.Rail(true, V(hx + 3.3f, hyy + .7f), V(hx + 9, hyy - 3.5f), V(hx + 14, hyy - 7), V(hx + 18, hyy - 8.5f));
        b.Rail(true, V(hx + 13, hyy - 4.4f), V(hx + 20, hyy - 5.4f), V(hx + 27, hyy - 7.6f), V(hx + 31, hyy - 6.8f));
        b.Orbs(hx + 14, hyy - 3.4f, hx + 26, hyy - 6.4f, 4);
        b.Plat(hx + 36, hyy - 9, 5);
        b.Pad(hx + 37, hyy - 8.45f, -25, 21);
        b.Blaster(hx + 36, hyy - 4, V(-1, -.5f), 1.3f, .5f, 30);
        b.Plat(hx + 50, hyy - 8, 4);
        b.Check(hx + 50, hyy - 6.6f);
        SpeedKick(b, hx + 52.3f, hyy - 8, true);
    }

    // CRIMSON RIFT: final boss over a bottomless rift; blue push shots, bullet rain.
    static void CrimsonRift(LevelBuilder b)
    {
        b.Start(-6, -2.5f);
        b.Plat(-5.65f, -4, 8.7f);
        b.Plat(5.65f, -4, 8.7f);
        b.Plat(-10.4f, .6f, .8f, 10);
        b.Plat(10.4f, .6f, .8f, 10);
        b.Plat(0, 6, 21.6f, .6f);
        b.Mover(0, -2.2f, 2.2f, 2f, 0, 5f);
        b.Tramp(-8.5f, -3.5f, 1.8f);
        b.Tramp(8.5f, -3.5f, 1.8f);
        b.Plat(-6, 1.4f, 3);
        b.Plat(6, 1.4f, 3);
        b.Rail(true, V(-3.6f, 3), V(0, 2), V(3.6f, 3));
        b.Blaster(0, 5.05f, V(0, -1), 3.2f, 1f, 25).glide = 8f;
        b.Label(0, 4.6f, "don't fall into the rift · J shields blue shots|don't fall · SHIELD blue shots");
        b.Rival(6, -2.5f, new Vector2(-10, -3.65f), new Vector2(10, 5.5f), 2, -1.3f, 1.3f);
    }
}
