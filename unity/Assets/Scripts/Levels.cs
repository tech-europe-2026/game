using System;
using UnityEngine;

public static class Levels
{
    public struct Level
    {
        public string name, unlock;
        public Action<LevelBuilder> build;
    }

    static Vector2 V(float x, float y) => new Vector2(x, y);

    public static readonly Level[] All =
    {
        new Level { name = "FIRST FLIGHT", unlock = "jump,crouch,dash", build = FirstFlight },
        new Level { name = "UPSIDE", unlock = "reverse,climb", build = Upside },
        new Level { name = "HEAVY WEATHER", unlock = "grow,parry", build = HeavyWeather },
        new Level { name = "PHASE SHIFT", unlock = "teleport,camo", build = PhaseShift },
    };

    static void FirstFlight(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(3, 0, 12);
        b.Label(3, 3, "A / D to roll   ·   SPACE to jump|left thumb rolls   ·   tap JUMP");
        b.Orbs(5, 1.3f, 8, 1.3f, 3);
        b.Orb(10.5f, 2.6f);
        b.Plat(16, 0, 8);
        b.Ramp(20, 0, 28, -3);
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

        b.Plat(91, 5, 3);
        b.Orb(91, 6.4f);
        b.Plat(95.5f, 4, 3);
        b.Plat(100.5f, 4, 6);
        b.Shards(100.5f, 4.35f, 3);
        b.Orb(100.5f, 6.4f);
        b.Rail(true, V(103.8f, 4.35f), V(108, 2.8f), V(114.8f, 3.3f));
        b.Orbs(106, 3.8f, 110, 3.8f, 3);
        b.Plat(118, 3, 6);
        b.Goal(119.5f, 5);
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
        b.Plat(47, 9.65f, 14);
        b.Orbs(42, 11, 46, 11, 3);

        b.Label(58, 12.2f, "flip · roll · flip");
        b.Plat(64, 15, 14);
        b.Spinner(64, 13.2f, 3, -140);
        b.Orb(60, 13.9f);
        b.Orb(68, 13.9f);
        b.Plat(78.5f, 10, 9);
        b.Check(80, 11.2f);

        b.Plat(84, 15, 2, 10);
        b.Orbs(82.3f, 12.5f, 82.3f, 17.5f, 3);
        b.Plat(92, 19.65f, 14);
        b.Shards(91, 20, 3);
        b.Orb(91, 22);
        b.Goal(97, 21.5f);
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
        b.Label(42, -1.4f, "Q to parry · send it back|PARRY · send it back");
        b.Orb(40, -3.8f);
        var crystal = b.Crystal(47, -4.15f);
        var door = b.Door(50, -2.3f, .8f, 4.7f);
        crystal.linked = door;
        b.Turret(55, -4.1f, Vector2.left, 1.6f);
        b.Orb(53, -3.8f);
        b.Pad(58.5f, -4.45f, 0, 24);
        b.Plat(64, 3, 8);
        b.Check(62, 4.5f);

        b.Plat(76, 3, 16);
        b.Turret(83, 3.9f, Vector2.left, 1.3f);
        b.Turret(76, 9.5f, Vector2.down, 1.5f, .7f);
        b.Orbs(70, 4.5f, 80, 4.5f, 4);
        b.Plat(89, 3, 4);
        b.Goal(89, 5.3f);
    }

    static void PhaseShift(LevelBuilder b)
    {
        b.Start(0, 1);
        b.Plat(4, 0, 12);
        b.Label(4, 3, "T to blink through walls|BLINK through walls");
        b.Plat(12, 4, 1.2f, 12);
        b.Plat(22, 0, 20);
        b.Orb(15.5f, 1.4f);

        b.Plat(26, 5, 10, 1.2f);
        b.Gate(24, .35f, 4.4f);
        b.Gate(28, .35f, 4.4f);
        b.Label(21, 7, "V to camouflage through lasers|CAMO through lasers");
        b.Orbs(23, 1.3f, 29, 1.3f, 3);

        b.Gate(33.5f, -3, 7);
        b.Label(33.5f, 8, "or blink past them");
        b.Plat(38, 0, 6);
        b.Mover(44, 1, 3, 0, 5, 4);
        b.Orbs(44, 3, 44, 7, 3);
        b.Plat(50, 6.5f, 6);

        b.Rail(true, V(53.3f, 6.83f), V(58, 5), V(64, 5.5f), V(69.8f, 8.3f));
        b.Gate(61, 5.8f, 9.5f);
        b.Orbs(56, 6.2f, 66, 6.8f, 4);
        b.Label(58, 10.5f, "mix your states");
        b.Plat(73, 8, 6);
        b.Check(73, 9.4f);

        b.Plat(79, 12, 1.2f, 10);
        b.Plat(84, 8, 8);
        b.Orb(82, 9.4f);
        b.Plat(95, 14, 12);
        b.Shards(95, 13.4f, 2, 180);
        b.Orbs(90, 12.4f, 100, 12.4f, 3);
        b.Plat(106, 8, 6);
        b.Goal(107, 10);
    }
}
