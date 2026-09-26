using System;
using UnityEngine;

public static class Levels
{
    public struct Level
    {
        public string name, unlock;
        public bool boss;
        public Color top, mid, bottom;
        public Action<LevelBuilder> build;
    }

    static Vector2 V(float x, float y) => new Vector2(x, y);
    static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    public static readonly Level[] All =
    {
        new Level { name = "FIRST FLIGHT", unlock = "jump,crouch,dash", build = FirstFlight, top = C(120, 170, 245), mid = C(186, 214, 255), bottom = C(255, 222, 214) },
        new Level { name = "UPSIDE", unlock = "jump,reverse,climb", build = Upside, top = C(124, 106, 226), mid = C(198, 180, 255), bottom = C(255, 206, 226) },
        new Level { name = "HEAVY WEATHER", unlock = "jump,grow", build = HeavyWeather, top = C(78, 170, 204), mid = C(172, 230, 236), bottom = C(238, 250, 244) },
        new Level { name = "PULSE", unlock = "jump,dash", build = PhaseShift, top = C(246, 134, 128), mid = C(255, 196, 170), bottom = C(255, 236, 204) },
        new Level { name = "DUEL", unlock = "jump,dash", boss = true, build = Duel, top = C(64, 60, 120), mid = C(170, 120, 190), bottom = C(255, 170, 160) },
    };

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

        b.Label(40, -.3f, "grind the rails");
        b.Rail(true, V(34.2f, -2.63f), V(40, -3.6f), V(46, -2.6f), V(51.8f, .3f));
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
        b.Orb(186, 3.4f);
        b.Plat(191.5f, 3, 5);
        b.Orbs(190.5f, 4.5f, 192.5f, 4.5f, 2);
        b.Plat(198.5f, 2.5f, 5);
        b.Gate(198.5f, 2.85f, 6, -.8f, true);
        b.Plat(205, 2.5f, 8);
        b.Plat(209.2f, 4.3f, .7f, 4f);
        b.Goal(206, 4.5f);
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

        b.Plat(38, 5, 2, 10);
        b.Label(33, 5, "hold C to climb|hold CLIMB against a wall");
        b.Orbs(36.3f, 3.5f, 36.3f, 8.5f, 3);
        b.Plat(46.5f, 9.65f, 15);
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

        b.Plat(129, 6, 6);
        b.Label(137, 9, "flip over the gap");
        b.Plat(137, 11, 12);
        b.Orbs(135, 10, 141, 10, 3);
        b.Plat(148, 6, 8);
        b.Gate(148, 6.35f, 9.5f, 0, true);
        b.Plat(154, 9, 1.2f, 8);
        b.Label(150, 11, "climb over");
        b.Plat(159, 12.65f, 9);
        b.Orb(159, 14);
        b.Rail(true, V(163.3f, 13.35f), V(168, 12), V(173, 12.8f));
        b.Orbs(166, 12.8f, 170, 12.8f, 2);
        b.Plat(177, 12.5f, 7);
        b.Plat(180.7f, 14.3f, .7f, 4f);
        b.Goal(177.5f, 14.5f);
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
        b.Ice(175, -1, 181, -3, 1);
        b.Plat(185, -3, 8);
        b.Gate(186, -2.65f, 1, .5f, true);
        b.Orb(183, -1.6f);
        b.Plat(193, -3, 8);
        b.Plat(197.2f, -1.2f, .7f, 4f);
        b.Goal(194, -1);
    }

    static void PhaseShift(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(4, 0, 12);
        b.Label(6, 3, "red lasers pulse · wait for the gap");
        b.Plat(22, 0, 20);
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

        b.Rail(true, V(53.3f, 6.83f), V(58, 5), V(64, 5.5f), V(69.8f, 8.3f));
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
        b.Plat(150.2f, 12.1f, .7f, 4f);
        b.Goal(147, 12.3f);
    }

    static void Duel(LevelBuilder b)
    {
        b.Start(-7, -2.5f);
        b.Plat(0, -4, 20);
        b.Plat(-10.4f, .6f, .8f, 10);
        b.Plat(10.4f, .6f, .8f, 10);
        b.Plat(0, 6, 21.6f, .6f);
        b.Plat(-5.5f, 0, 4);
        b.Plat(5.5f, 0, 4);
        b.Plat(0, 2.8f, 4);
        b.Label(0, -1.2f, "ram the red ball · dash hits hardest");
        b.Rival(6, -2.5f, new Vector2(-10, -3.65f), new Vector2(10, 5.5f));
    }
}
