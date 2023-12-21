using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Titan.DataConverters;
using Titan.Models;
using Titan.Utilities.Extensions;

namespace Titan.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFileSelected))]
        private string _SelectedFilePath = string.Empty;

        [ObservableProperty]
        private int _Progress = 0;

        public ObservableCollection<LogItem> Records { get; set; } = new();

        private readonly List<FriendlyRecord<(long, double)>> doubles = new();
        private readonly List<FriendlyRecord<(long, string)>> strings = new();

        public bool IsFileSelected
        {
            get => SelectedFilePath != string.Empty;
        }

        [RelayCommand]
        private async Task ConvertToSysIdJson()
        {
            Log.Information("Beginning Log Conversion.");

            Dictionary<string, object> rootJson = new()
            {
                { "sysid", true },
                { "test", "Simple" },
                { "units", "Rotations" },
                { "unitsPerRotation", 1.0 }
            };

            var stateRecord = strings.FirstOrDefault(p => p.Name == "State" || p.Name.Contains("sysid-test-state", StringComparison.InvariantCultureIgnoreCase));
            var velocityRecord = doubles.FirstOrDefault(p => p.Name == Records.FirstOrDefault(p => p.SelectedRecordType == "Velocity")?.Name);
            var positionRecord = doubles.FirstOrDefault(p => p.Name == Records.FirstOrDefault(p => p.SelectedRecordType == "Position")?.Name);
            var voltageRecord = doubles.FirstOrDefault(p => p.Name == Records.FirstOrDefault(p => p.SelectedRecordType == "Voltage")?.Name);

            if (stateRecord.Id == 0)
            {
                Log.Warning("State record is missing.");

                var box = MessageBoxManager.GetMessageBoxStandard(
                    "State record is missing",
                    "State record is missing, please verify you have selected a valid characterization datalog.",
                    windowStartupLocation: WindowStartupLocation.CenterOwner);

                _ = await box.ShowWindowDialogAsync(MainWindow.Instance);
                return;
            }

            if (velocityRecord.Id == 0)
            {
                Log.Information("Velocity signal is not selected or missing.");

                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Velocity signal is missing",
                    "Velocity is missing, please verify you have selected a signal as the Velocity signal.",
                    windowStartupLocation: WindowStartupLocation.CenterOwner);

                _ = await box.ShowWindowDialogAsync(MainWindow.Instance);
                return;
            }

            if (positionRecord.Id == 0)
            {
                Log.Information("Position signal is not selected or missing.");

                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Position signal is missing",
                    "Position is missing, please verify you have selected a signal as the Position signal.",
                    windowStartupLocation: WindowStartupLocation.CenterOwner);

                _ = await box.ShowWindowDialogAsync(MainWindow.Instance);
                return;
            }

            if (voltageRecord.Id == 0)
            {
                Log.Information("Voltage signal is not selected or missing.");

                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Voltage signal is missing",
                    "Voltage is missing, please verify you have selected a signal as the Voltage signal.",
                    windowStartupLocation: WindowStartupLocation.CenterOwner);

                _ = await box.ShowWindowDialogAsync(MainWindow.Instance);
                return;
            }

            if (stateRecord.Values.Count > 0)
            {
                var dataEntries = new EntryContainer(velocityRecord.Values, positionRecord.Values, voltageRecord.Values);

                rootJson["fast-forward"] = GetTestFrames("dynamic-forward", stateRecord.Values, dataEntries);
                rootJson["fast-backward"] = GetTestFrames("dynamic-reverse", stateRecord.Values, dataEntries);
                rootJson["slow-forward"] = GetTestFrames("quasistatic-forward", stateRecord.Values, dataEntries);
                rootJson["slow-backward"] = GetTestFrames("quasistatic-reverse", stateRecord.Values, dataEntries);
            }
            else
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "State record is missing",
                    "State record is missing, please verify you have selected a valid characterization datalog.",
                    windowStartupLocation: WindowStartupLocation.CenterOwner);

                _ = await box.ShowWindowDialogAsync(MainWindow.Instance);

                return;
            }

            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(MainWindow.Instance);

            if (topLevel != null)
            {
                // Start async operation to open the dialog.
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save SysID Json"
                });

                if (file is not null)
                {
                    await using FileStream createStream = File.Create(file.Path.AbsolutePath);
                    await JsonSerializer.SerializeAsync(createStream, rootJson);
                }
            }
        }

        [RelayCommand]
        private async Task SelectFile()
        {
            var topLevel = TopLevel.GetTopLevel(MainWindow.Instance);

            if (topLevel == null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open DataLog",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>()
                {
                    new("DataLog")
                    {
                        Patterns = new List<string>()
                        {
                            "*.wpilog",
                        }
                    }
                }
            });

            if (files.Count > 0)
            {
                SelectedFilePath = files.First().Path.AbsolutePath;
            }

            AnalyzeFile();
        }

        private List<double[]> GetTestFrames(string testname, List<(long, string)> stateEntries, EntryContainer dataEntries)
        {
            Log.Information($"Retrieving test frames for {testname}.");

            var frames = new List<double[]>();

            var testStateEntries = stateEntries.OrderBy(p => p.Item1).Where(p => p.Item2 == testname).ToList();

            long minTimestamp = testStateEntries.FirstOrDefault().Item1;
            long maxTimestamp = testStateEntries.LastOrDefault().Item1;

            var velocityInRangeEntries = dataEntries.VelocityEntries
                .Where(p => p.Item1 > minTimestamp && p.Item1 < maxTimestamp).OrderBy(p => p.Item1).ToList();
            var positionInRangeEntries = dataEntries.PositionEntries
                .Where(p => p.Item1 > minTimestamp && p.Item1 < maxTimestamp).OrderBy(p => p.Item1).ToList();
            var voltageInRangeEntries = dataEntries.VoltageEntries
                .Where(p => p.Item1 > minTimestamp && p.Item1 < maxTimestamp).OrderBy(p => p.Item1).ToList();

            var velocityAvgDelta = velocityInRangeEntries.Select(p => p.Item1).AverageDifference();
            var positionAvgDelta = positionInRangeEntries.Select(p => p.Item1).AverageDifference();
            var voltageAvgDelta = voltageInRangeEntries.Select(p => p.Item1).AverageDifference();

            Log.Information($"Velocity Avg Delta: {velocityAvgDelta}");
            Log.Information($"Position Avg Delta: {positionAvgDelta}");
            Log.Information($"Voltage Avg Delta: {voltageAvgDelta}");

            // Compute based on smallest delta. Is velocity the smallest
            if (velocityAvgDelta < positionAvgDelta && velocityAvgDelta < voltageAvgDelta)
            {
                for (int i = 0; i < velocityInRangeEntries.Count; i++)
                {
                    var entry = velocityInRangeEntries[i];
                    var frame = new double[4];

                    frame[0] = entry.Item1 * 0.000001; // set timestamp equal to our most frequent signal
                    frame[1] = voltageInRangeEntries.GetInterpolatedValue(entry.Item1); // get interpolated voltage value
                    frame[2] = positionInRangeEntries.GetInterpolatedValue(entry.Item1); // get interpolated position value
                    frame[3] = entry.Item2; // no need to interpolate, this is our time base

                    frames.Add(frame);
                }
            }
            else if (positionAvgDelta < voltageAvgDelta && positionAvgDelta < velocityAvgDelta) // Position?
            {
                for (int i = 0; i < positionInRangeEntries.Count; i++)
                {
                    var entry = positionInRangeEntries[i];
                    var frame = new double[4];

                    frame[0] = entry.Item1 * 0.000001;
                    frame[1] = voltageInRangeEntries.GetInterpolatedValue(entry.Item1);
                    frame[2] = entry.Item2; // get interpolated position value
                    frame[3] = velocityInRangeEntries.GetInterpolatedValue(entry.Item1);

                    frames.Add(frame);
                }
            }
            else // Voltage
            {
                for (int i = 0; i < voltageInRangeEntries.Count; i++)
                {
                    var entry = voltageInRangeEntries[i];
                    var frame = new double[4];

                    frame[0] = entry.Item1 * 0.000001;
                    frame[1] = entry.Item2;
                    frame[2] = positionInRangeEntries.GetInterpolatedValue(entry.Item1); // get interpolated position value
                    frame[3] = velocityInRangeEntries.GetInterpolatedValue(entry.Item1);

                    frames.Add(frame);
                }
            }

            Log.Information($"Built {frames.Count} frames.");
            return frames;
        }

        private void AnalyzeFile()
        {
            Log.Information($"Importing datalog at {SelectedFilePath}.");

            SharedViewModel.Instance.IsGlobalBusy = true;

            Records.Clear();
            doubles.Clear();
            strings.Clear();

            Progress = 0;

            _ = Task.Run(() =>
            {
                try
                {
                    var datalogReader = new DataLogReader(SelectedFilePath);

                    if (datalogReader.IsValid())
                    {
                        datalogReader.ProgressChanged += DatalogReader_ProgressChanged;

                        var records = datalogReader.GetRecords();

                        Log.Information($"Datalog contains {records.Count} records");

                        foreach (var record in records)
                        {
                            if (record.IsStart())
                            {
                                var data = record.GetStartData();

                                if (data.Type == "double")
                                {
                                    doubles.Add(new(data.Entry, data.Name));
                                }
                                else if (data.Type == "string")
                                {
                                    strings.Add(new(data.Entry, data.Name));
                                }

                                if (!Records.Any(p => p.Name == data.Name))
                                {
                                    Records.Add(new(data.Name, false));
                                }
                            }
                            else if (record.IsSetMetadata())
                            {
                                Log.Information("Record is metadata record");
                                var data = record.GetSetMetadataData();
                            }
                            else if (record.IsFinish())
                            {
                                Log.Information("Record is finish record");
                            }
                            else
                            {
                                var doubleEntry = doubles.Where(p => p.Id == record.Entry).FirstOrDefault();

                                if (doubleEntry.Id != 0)
                                {
                                    doubleEntry.Values.Add((record.Timestamp, record.GetDouble()));
                                }

                                var stringEntry = strings.Where(p => p.Id == record.Entry).FirstOrDefault();

                                if (stringEntry.Id != 0)
                                {
                                    stringEntry.Values.Add((record.Timestamp, record.GetString()));
                                }
                            }
                        }

                        Log.Information("Finished importing all records.");
                    } else
                    {
                        Log.Warning("Invalid file selected as datalog.");

                        Dispatcher.UIThread.Invoke(new Action(async () =>
                        {
                            var box = MessageBoxManager.GetMessageBoxStandard(
                                "Invalid datalog",
                                $"The selected file could not be parsed as a datalog.",
                                windowStartupLocation: WindowStartupLocation.CenterOwner);

                            _ = await box.ShowWindowDialogAsync(MainWindow.Instance);
                        }));
                    }

                    datalogReader.ProgressChanged -= DatalogReader_ProgressChanged;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception occurred during datalog import.");

                    Dispatcher.UIThread.Invoke(new Action(async () =>
                    {
                        var box = MessageBoxManager.GetMessageBoxStandard(
                            "Exception occurred",
                            $"An exception occurred while importing datalog. \n\n {ex.Message}",
                            windowStartupLocation: WindowStartupLocation.CenterOwner);

                        _ = await box.ShowWindowDialogAsync(MainWindow.Instance);
                    }));
                }

                SharedViewModel.Instance.IsGlobalBusy = false;
            });
        }

        private void DatalogReader_ProgressChanged(object sender, DataLogReader.ProgressChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() => Progress = (int)(e.Progress * 100.0));

        }
    }
}
