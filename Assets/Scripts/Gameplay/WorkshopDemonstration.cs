using System.Collections.Generic;
using UnityEngine;

namespace LetGo
{
    public enum LearnedHabitKind { None, Pause, Jump, SeekHelp }

    /// <summary>The action actually used on the last completed trial, safe to carry between scenes.</summary>
    public readonly struct LearnedHabitSnapshot
    {
        public LearnedHabitKind Kind { get; }
        public float PauseSeconds { get; }
        public float PositionX { get; }
        public string HelpKind { get; }
        public int Demonstrations { get; }
        public LearnedHabitSnapshot(LearnedHabitKind kind, float pauseSeconds, float positionX, string helpKind, int demonstrations)
        { Kind = kind; PauseSeconds = pauseSeconds; PositionX = positionX; HelpKind = helpKind ?? ""; Demonstrations = demonstrations; }
    }

    public readonly struct DemonstratedAction
    {
        public LearnedHabitKind Kind { get; }
        public float X { get; }
        public float Seconds { get; }
        public float LandingX { get; }
        public float Height { get; }
        public string Help { get; }
        public bool SelfSteadied { get; }
        public bool SteadiesInDark => Kind == LearnedHabitKind.Pause && X >= 17f && X <= 19.5f && Seconds >= 1.2f && SelfSteadied && string.IsNullOrEmpty(Help);
        public bool CrossesGap => Kind == LearnedHabitKind.Jump && X <= 15.4f && LandingX >= 16.65f;
        public DemonstratedAction(LearnedHabitKind kind, float x, float seconds, float landingX = 0f, float height = 0f, string help = "", bool selfSteadied = false)
        { Kind = kind; X = x; Seconds = seconds; LandingX = landingX; Height = height; Help = help; SelfSteadied = selfSteadied; }
    }

    /// <summary>Observes real movement. It stores decisions at places, never an autoplay path.</summary>
    public sealed class WorkshopDemonstration
    {
        private readonly List<DemonstratedAction> actions = new();
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<DemonstratedAction> view;
        private float stillSeconds, stillX, lastX, takeoffX, takeoffY, peakY, flightSeconds;
        private string stillHelp;
        private bool inFlight, descended, stillSelfSteadied;
        public bool Recording { get; private set; }
        public float FurthestX { get; private set; }
        public float Elapsed { get; private set; }
        public IReadOnlyList<DemonstratedAction> Actions => view;
        public WorkshopDemonstration() => view = actions.AsReadOnly();

        public void Begin(Vector2 position)
        {
            actions.Clear();
            Recording = true;
            FurthestX = lastX = position.x;
            stillSeconds = flightSeconds = Elapsed = 0f;
            inFlight = descended = stillSelfSteadied = false;
            stillHelp = "";
        }

        public void Sample(float dt, Vector2 position, Vector2 velocity, bool jumpAccepted, string help, bool selfSteadied = false)
        {
            if (!Recording || dt <= 0f) return;
            Elapsed += dt;
            FurthestX = Mathf.Max(FurthestX, position.x);
            if (jumpAccepted && velocity.y > 1f && !inFlight)
            {
                FlushPause();
                inFlight = true;
                descended = false;
                takeoffX = position.x; takeoffY = position.y; peakY = position.y; flightSeconds = 0f;
            }
            if (inFlight)
            {
                flightSeconds += dt;
                peakY = Mathf.Max(peakY, position.y);
                if (velocity.y < -0.2f) descended = true;
                if (descended && flightSeconds > 0.12f && Mathf.Abs(velocity.y) < 0.15f)
                {
                    if (peakY - takeoffY > 0.2f && position.x > takeoffX + 0.25f)
                        Add(new DemonstratedAction(LearnedHabitKind.Jump, takeoffX, flightSeconds, position.x, peakY - takeoffY));
                    inFlight = false;
                }
            }
            var stopped = !inFlight && Mathf.Abs(velocity.x) < 0.15f && Mathf.Abs(velocity.y) < 0.15f &&
                Mathf.Abs(position.x - lastX) < 0.025f && position.x >= 13.5f;
            if (stopped)
            {
                if (stillSeconds == 0f) { stillX = position.x; stillHelp = help ?? ""; }
                if (!string.IsNullOrEmpty(help)) stillHelp = help;
                stillSelfSteadied |= selfSteadied;
                stillSeconds += dt;
            }
            else FlushPause();
            // A carried source teaches reliance on that source at the entrance to darkness.
            if (position.x >= 17.4f && position.x < 21.5f && !string.IsNullOrEmpty(help))
                Add(new DemonstratedAction(LearnedHabitKind.SeekHelp, 17.8f, 0f, help: help));
            lastX = position.x;
        }

        public bool Finish()
        {
            if (!Recording) return false;
            FlushPause();
            Recording = false;
            actions.Sort((left, right) => left.X.CompareTo(right.X));
            return FurthestX >= 16.65f && Elapsed >= 1f;
        }

        public void Cancel() { Recording = false; actions.Clear(); }

        private void FlushPause()
        {
            if (stillSeconds >= 0.4f)
                Add(new DemonstratedAction(LearnedHabitKind.Pause, stillX, Mathf.Min(3f, stillSeconds), help: stillHelp, selfSteadied: stillSelfSteadied));
            stillSeconds = 0f;
            stillHelp = "";
            stillSelfSteadied = false;
        }

        private void Add(DemonstratedAction action)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Kind != action.Kind || Mathf.Abs(actions[i].X - action.X) > 0.6f) continue;
                // Revisiting a place during a demonstration replaces what was shown there.
                actions[i] = action;
                return;
            }
            if (actions.Count < 24) actions.Add(action);
        }
    }
}
