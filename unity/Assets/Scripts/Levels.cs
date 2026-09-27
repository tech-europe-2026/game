using System;
using UnityEngine;

public static class Levels
{
    public struct Level
    {
        public string name, unlock;
        public bool boss;
        public Color top, mid, bottom;
        public Vector2 camMin, camMax;
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
    };

    // Each finale strings three different rail shapes together; the style picks which three.
    static void RailRun(LevelBuilder b, float x, float py, int style = 0)
    {
        float y = py + .7f;
        Vector2 P(float dx, float dy) => V(x + dx, y + dy);
        Seg(b, P(0, 0), P(7.2f, -.55f), style % 5);
        Seg(b, P(10.7f, -1.75f), P(17.2f, -2.25f), (style + 2) % 5);
        Seg(b, P(20.7f, -3.15f), P(29.2f, -4f), (style + 4) % 5);
        b.Orbs(x + 3, y - .6f, x + 22, y - 3.6f, 5);
        b.Plat(x + 33.2f, py - 3.6f, 8);
        b.Plat(x + 37.4f, py - 1.8f, .7f, 4f);
        b.Goal(x + 34.2f, py - 1.6f);
    }

    static void Seg(LevelBuilder b, Vector2 a, Vector2 e, int shape)
    {
        float m = Mathf.Min(a.y, e.y), L = e.x - a.x;
        Vector2 Q(float t, float dy) => V(a.x + L * t, m + dy);
        switch (shape)
        {
            case 1: b.Rail(true, a, Q(.22f, -2.2f), Q(.65f, -1.9f), e); break;           // drop + kicker
            case 2: b.Rail(true, a, Q(.3f, -1.4f), Q(.5f, -.6f), Q(.72f, -1.3f), e); break; // camel humps
            case 3: b.Rail(true, a, Q(.4f, -.5f), Q(.78f, -2.1f), e); break;              // late plunge
            case 4: b.Rail(true, a, Q(.2f, -1f), Q(.4f, -.35f), Q(.6f, -1.1f), Q(.8f, -.4f), e); break; // wave
            default: b.Rail(true, a, Q(.5f, -1.3f), e); break;                             // dip
        }
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
        float y = py + .7f, ly = y - 2.5f, xl = x + 9, xe = xl + shift * turns + 5;
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
        b.Start(0, 1);
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
        b.Frost(170, -4.15f, -.5f, 0, true);
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
        b.Plat(-8.3f, 1.6f, 3);
        b.Plat(8.3f, 1.6f, 3);
        b.Rail(true, V(-6.7f, 2.2f), V(0, .2f), V(6.7f, 2.2f));
        b.Label(0, 4.3f, "slam it from above · jump the shockwaves");
        b.Rival(4, -2.5f, new Vector2(-10, -3.65f), new Vector2(10, 5.5f), 1);
    }
}
