using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Titan.DataConverters;
using Titan.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            var selectedrecords = Records.Where(p => p.IsSelected).ToList();

            if (selectedrecords.Count == 0)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("No data is selected", "Please select supply voltage, position, and velocity for characterized mechanisms");
                _ = await box.ShowAsync();

                return;
            }

            Dictionary<string, object> rootJson = new();
            rootJson.Add("sysid", true);
            rootJson.Add("test", "Simple");
            rootJson.Add("units", "Rotations");
            rootJson.Add("unitsPerRotation", 1.0);

            var staterecord = strings.FirstOrDefault(p => p.Name == "State");
            var velocityrecord = doubles.FirstOrDefault(p => p.Name == Records.First(p => p.SelectedRecordType == "Velocity").Name);
            var positionrecord = doubles.FirstOrDefault(p => p.Name == Records.First(p => p.SelectedRecordType == "Position").Name);
            var voltagerecord = doubles.FirstOrDefault(p => p.Name == Records.First(p => p.SelectedRecordType == "Voltage").Name);

            if (staterecord.Values.Count > 0)
            {
                var allentries = new Dictionary<string, List<double[]>>();
                var values = staterecord.Values.OrderBy(p => p.Item1).ToList();

                bool hasSetDynamicForward = false;
                bool hasSetDynamicReverse = false;
                bool hasSetQuasiForward = false;
                bool hasSetQuasiReverse = false;

                int i = 0;
                foreach (var record in values)
                {

                    var voltage = voltagerecord.Values.Aggregate((x, y) => Math.Abs(x.Item1 - record.Item1) < Math.Abs(y.Item1 - record.Item1) ? x : y).Item2;
                    var timestamp = record.Item1;

                    var entry = new double[4];
                    entry[0] = (double)timestamp * 0.001; // item 1 is timestamp
                    entry[1] = voltage * 12;
                    entry[2] = positionrecord.Values.Aggregate((x, y) => Math.Abs(x.Item1 - record.Item1) < Math.Abs(y.Item1 - record.Item1) ? x : y).Item2;
                    entry[3] = velocityrecord.Values.Aggregate((x, y) => Math.Abs(x.Item1 - record.Item1) < Math.Abs(y.Item1 - record.Item1) ? x : y).Item2;

                    if (record.Item2 == "dynam" && voltage > 0)
                    {
                        if (!hasSetDynamicForward)
                        {
                            hasSetDynamicForward = true;
                            allentries.Add("fast-forward", new());
                        }

                        allentries["fast-forward"].Add(entry);
                    } else if (record.Item2 == "dynam" && voltage < 0)
                    {
                        if (!hasSetDynamicReverse)
                        {
                            hasSetDynamicReverse = true;
                            allentries.Add("fast-backward", new());
                        }

                        allentries["fast-backward"].Add(entry);
                    } else if (record.Item2 == "quasi" && voltage > 0)
                    {
                        if (!hasSetQuasiForward)
                        {
                            hasSetQuasiForward = true;
                            allentries.Add("slow-forward", new());
                        }

                        allentries["slow-forward"].Add(entry);
                    } else if (record.Item2 == "quasi" && voltage < 0)
                    {
                        if (!hasSetQuasiReverse)
                        {
                            hasSetQuasiReverse = true;
                            allentries.Add("slow-backward", new());
                        }

                        allentries["slow-backward"].Add(entry);
                    }

                    i++;
                }

                foreach (var entry in allentries)
                {
                    rootJson.Add(entry.Key, entry.Value);
                }
            } else
            {
                var box = MessageBoxManager.GetMessageBoxStandard("State record is missing", "State record is missing, please verify you have selected a valid characterization datalog.");
                _ = await box.ShowAsync();

                return;
            }

            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(MainWindow.Instance);

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

        private void AnalyzeFile()
        {
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
                                Debug.WriteLine("Record is metadata record");
                                var data = record.GetSetMetadataData();
                            }
                            else if (record.IsFinish())
                            {
                                Debug.WriteLine("Record is finish record");
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
                    }

                    datalogReader.ProgressChanged -= DatalogReader_ProgressChanged;
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Invoke(new Action(async () =>
                    {
                        var box = MessageBoxManager.GetMessageBoxStandard("Exception occurred", $"An exception occurred while analyzing datalog. \n\n {ex.Message}");
                        _ = await box.ShowAsync();
                    }));
                }

                SharedViewModel.Instance.IsGlobalBusy = false;
            });
        }

        private void DatalogReader_ProgressChanged(object sender, DataLogReader.ProgressChangedEventArgs e)
        {
            Progress = (int)(e.Progress * 100.0);
        }
    }
}
