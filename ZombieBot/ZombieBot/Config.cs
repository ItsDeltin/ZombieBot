﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using Deltin.CustomGameAutomation;

namespace ZombieBot
{
    class Config
    {
        public static Config ParseConfig()
        {
            string configLocation = Extra.GetExecutingDirectory() + "config.xml";

            if (!File.Exists(configLocation))
            {
                Log("Could not find config file, using defaults.");
                return new Config();
            }

            XDocument document = XDocument.Load(configLocation);

            return new Config()
            {
                Version = ParseString(document, "version", "elim", "tdm") == "elim" ? 0 : 1,
                MinimumPlayers = ParseInt(document, "minimumPlayers", min: 0, max: 7, @default: 5),
                DefaultMode = ParseString(document, "defaultMode", "abyxa", "serverbrowser", "private"),
                OverwatchEvent = ParseString(document, "overwatchEvent", OWEvent.None),
                ScreenshotMethod = ParseString(document, "screenshotMethod", ScreenshotMethod.BitBlt),
                Preset = ParseString(document, "preset", @default:"")
                    .Trim(),

                Name = ParseString(document, "name", "Zombies - Infection"),
                Region = ParseString(document, "region", "us", "eu", "kr"),

                BattlenetExecutable = ParseString(document, "battlenetExecutable", new OverwatchInfoAuto().BattlenetExecutableFilePath),
                OverwatchSettingsFile = ParseString(document, "overwatchSettingsFile", new OverwatchInfoAuto().OverwatchSettingsFilePath)
                    .Replace("{username}", Environment.UserName),

                Local = Exists(document, "local")
            };
        }

        private static void Log(string text)
        {
            Console.WriteLine("[Config] " + text);
        }

        private static string ParseString(XDocument document, string name, params string[] validValues)
        {
            string elementValue = document.Element("config")?.Element(name)?.Value?.ToLower();
            if (elementValue == null || !validValues.Contains(elementValue))
            {
                Log($"{name} is not {string.Join(", ", validValues)}. Using {validValues[0]} by default.");
                return validValues[0];
            }
            return elementValue;
        }

        private static string ParseString(XDocument document, string name, string @default)
        {
            string elementValue = document.Element("config")?.Element(name)?.Value?.ToLower();
            if (elementValue == null)
            {
                Log($"Could not get {name}. Using {@default} by default.");
                return @default;
            }
            return elementValue;
        }

        private static T ParseString<T>(XDocument document, string name, T @default) where T : struct
        {
            T value;
            if (!Enum.TryParse(document.Element("config")?.Element(name)?.Value, true, out value))
            {
                Log($"Could not get {name}. Using {@default} by default.");
                value = @default;
            }
            return value;
        }

        private static int ParseInt(XDocument document, string name, int min, int max, int @default)
        {
            int value;
            if (int.TryParse(document.Element("config")?.Element(name)?.Value, out value))
            {
                if (value < min || value > max)
                {
                    Log($"{name} ({value}) is less than {min} or greater than {max}. Using {@default} by default.");
                    value = @default;
                }
            }
            else
            {
                Log($"Could not get {name}. Using {@default} by default.");
                value = @default;
            }
            return value;
        }

        private static bool ParseBool(XDocument document, string name)
        {
            return ParseString(document, name, "false", "true") == "true";
        }

        private static bool Exists(XDocument document, string name)
        {
            return document.Element("config")?.Element(name) != null;
        }

        public int Version;
        public int MinimumPlayers;
        public string DefaultMode;
        public OWEvent OverwatchEvent;
        public ScreenshotMethod ScreenshotMethod;
        public string Preset;

        public string Name;
        public string Region;

        public string BattlenetExecutable;
        public string OverwatchSettingsFile;

        public bool Local;
    }
}
