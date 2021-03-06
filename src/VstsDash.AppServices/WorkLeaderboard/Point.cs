﻿using System;

namespace VstsDash.AppServices.WorkLeaderboard
{
    public class Point
    {
        public Point(TeamMemberPointType type, double value, string id, string description, DateTimeOffset earnedAt)
        {
            Type = type;
            Value = value;
            Id = id;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            EarnedAt = earnedAt;
        }

        public DateTimeOffset EarnedAt { get; }

        public string Description { get; }

        public string Id { get; }

        public TeamMemberPointType Type { get; }

        public double Value { get; }
    }
}