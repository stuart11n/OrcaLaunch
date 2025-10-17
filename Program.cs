using System;
using System.Drawing;
using System.Management;
using System.Windows.Forms;
using System.Diagnostics;

// A simple structure to hold the information needed to relaunch the process.
public class ProcessRelaunchInfo
{
    public string ExecutablePath { get; set; }
    public string DatadirPath { get; set; }
}

// This class represents your main Windows Form
public class ProcessParameterFinderForm : Form
{
    // --- UI Components ---
    private RichTextBox outputTextBox;
    // Removed 'findButton' as the scan is automatic
    private Label infoLabel;
    private FlowLayoutPanel buttonsPanel; // Container for dynamic buttons
    private Label instructionsLabel; // Label above dynamic buttons
    private Label toggleLogLabel; // NEW: Label to toggle log visibility

    // Store arguments passed to this application to be appended to the relaunch command
    private string currentAppArguments = string.Empty;

    // The name of the process we are looking for.
    private const string TargetProcessName = "Orca-Slicer.exe";

    public ProcessParameterFinderForm()
    {
        // 1. Get arguments passed to this application upon startup
        string[] args = Environment.GetCommandLineArgs();
        // Skip the first argument (which is the path to our own executable)
        if (args.Length > 1)
        {
            // Recombine all arguments into a single string, suitable for appending.
            // Note: We rely on Process.Start handling the quoting of the arguments string later.
            currentAppArguments = string.Join(" ", args, 1, args.Length - 1);
        }

        InitializeComponent();
        this.Text = "OrcaSlicer Launcher";
        this.BackColor = Color.FromArgb(245, 247, 250);
        this.Font = new Font("Segoe UI", 10);

        // Display the arguments we will be appending
        infoLabel.Text += $"\n(Current App Args to Append: {(string.IsNullOrEmpty(currentAppArguments) ? "None" : currentAppArguments)})";
    }

    private void InitializeComponent()
    {
        // Setup Form
        this.ClientSize = new Size(700, 100); // Increased size
        this.MinimumSize = new Size(500, 100);
        this.Padding = new Padding(15);
        this.AutoScroll = true;

        // Info Label (Instructions/Status)
        infoLabel = new Label
        {
            Text = $"Searching for processes named: {TargetProcessName}\n(Note: This requires System.Management and may need Administrator privileges.)",
            Dock = DockStyle.Top,
            Height = 60, // Increased height to show current app args
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(49, 114, 204),
        };

        // Find Button (REMOVED)

        // Instructions Label for dynamic buttons
        instructionsLabel = new Label
        {
            Text = "Found Instances:",
            Dock = DockStyle.Top,
            Height = 25,
            Padding = new Padding(0, 5, 0, 0),
            Visible = false, // Hide until processes are found
            Font = new Font(this.Font, FontStyle.Bold),
            ForeColor = Color.Black
        };

        // FlowLayoutPanel for Dynamic Buttons
        buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(5),
            BackColor = Color.FromArgb(230, 235, 240), // Light gray background
            BorderStyle = BorderStyle.FixedSingle,
            MinimumSize = new Size(100, 50)
        };

        // Toggle Log Label (NEW)
        toggleLogLabel = new Label
        {
            Text = "Show Scan Log Details",
            Dock = DockStyle.Top,
            Height = 20,
            ForeColor = Color.Gray,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font(this.Font.FontFamily, 9, FontStyle.Underline)
        };
        toggleLogLabel.Click += ToggleLogLabel_Click;


        // Output Text Box (for logs and detailed info)
        outputTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Multiline = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Padding = new Padding(5),
            Text = "Click the link above to view the scan details.",
        };

        // Use a TableLayoutPanel for modern, responsive layout control
        TableLayoutPanel layoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5, // Now 5 rows
            RowStyles =
            {
                //new RowStyle(SizeType.Absolute, 60),   // Row 0: infoLabel
                //new RowStyle(SizeType.Absolute, 25),   // Row 1: instructionsLabel
                new RowStyle(SizeType.AutoSize),       // Row 2: buttonsPanel (dynamic height)
                //new RowStyle(SizeType.Absolute, 25),   // Row 3: toggleLogLabel (NEW)
                //new RowStyle(SizeType.Absolute, 0)     // Row 4: outputTextBox (Collapsed by default)
            }
        };

        //layoutPanel.Controls.Add(infoLabel, 0, 0);
        //layoutPanel.Controls.Add(instructionsLabel, 0, 1);
        layoutPanel.Controls.Add(buttonsPanel, 0, 2);
        //layoutPanel.Controls.Add(toggleLogLabel, 0, 3);
        //layoutPanel.Controls.Add(outputTextBox, 0, 4);

        this.Controls.Add(layoutPanel);
        // findButton removed
    }

    /// <summary>
    /// Overrides OnLoad to trigger the process scan immediately when the form is displayed.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Automatically start the scan process
        FindButton_Click(this, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles the visibility/height of the output log box.
    /// </summary>
    private void ToggleLogLabel_Click(object sender, EventArgs e)
    {
        TableLayoutPanel layoutPanel = (TableLayoutPanel)toggleLogLabel.Parent;

        // Row 4 holds the outputTextBox
        RowStyle outputRowStyle = layoutPanel.RowStyles[4];

        layoutPanel.SuspendLayout();

        if (outputRowStyle.Height == 0)
        {
            // Expand
            outputRowStyle.SizeType = SizeType.Percent;
            outputRowStyle.Height = 100;
            toggleLogLabel.Text = "Hide Scan Log Details";
        }
        else
        {
            // Collapse
            outputRowStyle.SizeType = SizeType.Absolute;
            outputRowStyle.Height = 0;
            toggleLogLabel.Text = "Show Scan Log Details";
        }

        layoutPanel.ResumeLayout(true);
    }

    // --- Core Logic (Renamed from FindButton_Click conceptually) ---

    private void FindButton_Click(object sender, EventArgs e)
    {
        outputTextBox.Clear();
        buttonsPanel.Controls.Clear();
        instructionsLabel.Visible = false;

        outputTextBox.AppendText($"Searching for '{TargetProcessName}'...\n\n");

        // WQL query now selects both ProcessId and CommandLine
        // We only need CommandLine, but ProcessId is harmless.
        string wmiQuery = $"SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = '{TargetProcessName}'";
        int instancesWithDatadir = 0;

        try
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery))
            {
                ManagementObjectCollection processes = searcher.Get();

                if (processes.Count == 0)
                {
                    outputTextBox.AppendText($"No processes named '{TargetProcessName}' found running.");
                }
                else
                {
                    outputTextBox.AppendText($"Found {processes.Count} total instance(s).\n\n");

                    foreach (ManagementObject process in processes)
                    {
                        uint processId = (uint)process["ProcessId"];
                        string commandLine = process["CommandLine"]?.ToString() ?? string.Empty;

                        outputTextBox.AppendText($"--- PID {processId} ---\n");
                        outputTextBox.AppendText($"Full Command Line: {commandLine}\n");

                        if (!string.IsNullOrEmpty(commandLine))
                        {
                            string datadirArgumentPath = ParseDatadirArgument(commandLine);
                            string executablePath = ExtractExecutablePath(commandLine);

                            if (!string.IsNullOrEmpty(datadirArgumentPath) && !string.IsNullOrEmpty(executablePath))
                            {
                                instancesWithDatadir++;

                                // 1. Get the last part of the parameter (folder name) for button text
                                string folderName = GetLastPathComponent(datadirArgumentPath);

                                // 2. Create the data object to store relaunch info
                                var relaunchInfo = new ProcessRelaunchInfo
                                {
                                    ExecutablePath = executablePath,
                                    DatadirPath = datadirArgumentPath
                                };

                                // 3. Create the dynamic button
                                Button processButton = new Button
                                {
                                    Text = folderName,
                                    Tag = relaunchInfo, // Store the relaunch info in the Tag
                                    Width = 180,
                                    Height = 35,
                                    Margin = new Padding(5),
                                    BackColor = Color.FromArgb(77, 137, 219), // Medium Blue
                                    ForeColor = Color.White,
                                    FlatStyle = FlatStyle.Flat,
                                    Cursor = Cursors.Hand
                                };
                                processButton.FlatAppearance.BorderSize = 0;
                                processButton.Click += ProcessButton_Click;

                                buttonsPanel.Controls.Add(processButton);
                                outputTextBox.AppendText($"DATADIR Found: {datadirArgumentPath}\n");
                                outputTextBox.AppendText($"Executable Path: {executablePath}\n");
                                outputTextBox.AppendText($"Created relaunch button: '{folderName}'.\n\n");
                            }
                            else
                            {
                                outputTextBox.AppendText("Could not find both DATADIR parameter and Executable Path for this instance.\n\n");
                            }
                        }
                        else
                        {
                            outputTextBox.AppendText("Command Line property was null or empty. (Possible reason: Insufficient permissions, try running as Administrator).\n\n");
                        }
                    }

                    instructionsLabel.Visible = instancesWithDatadir > 0;
                    if (instancesWithDatadir == 0)
                    {
                        outputTextBox.AppendText("\nNo OrcaSlicer instances were found with a specific 'datadir' parameter that could be parsed for relaunching.\n");
                    }
                }
            }
        }
        catch (ManagementException ex)
        {
            outputTextBox.AppendText("--- WMI ERROR ---\n");
            outputTextBox.AppendText($"A WMI error occurred: {ex.Message}\n");
            outputTextBox.AppendText("Check if you have added the System.Management reference and if you are running the application as an Administrator.\n");
        }
        catch (Exception ex)
        {
            outputTextBox.AppendText("--- UNEXPECTED ERROR ---\n");
            outputTextBox.AppendText($"An unexpected error occurred: {ex.Message}\n");
        }

        outputTextBox.AppendText("--------------------------------------------------\n");
        outputTextBox.AppendText("Scan complete.");
    }

    /// <summary>
    /// Event handler for the dynamically created buttons. Relaunches the process.
    /// </summary>
    private void ProcessButton_Click(object sender, EventArgs e)
    {
        Button clickedButton = (Button)sender;
        if (clickedButton.Tag is ProcessRelaunchInfo info)
        {
            RelaunchProcess(info);
        }
    }

    /// <summary>
    /// Relaunches the target process with the specific datadir argument and this application's arguments.
    /// </summary>
    private void RelaunchProcess(ProcessRelaunchInfo info)
    {
        try
        {
            // 1. Prepare the datadir argument. Quoting the path handles spaces.
            string datadirArg = $"--datadir \"{info.DatadirPath}\"";

            // 2. Combine the arguments: datadir argument + parent application arguments
            // We use the datadir argument first to ensure it takes precedence.
            string finalArgs = $"{datadirArg} {currentAppArguments}".Trim();

            // Use Process.Start to launch the application
            Process.Start(info.ExecutablePath, finalArgs);

            outputTextBox.AppendText($"\nRELAUNCHED: '{info.ExecutablePath}'\nArguments: {finalArgs}\n");

            Application.Exit();
        }
        catch (Exception ex)
        {
            outputTextBox.AppendText($"\nERROR Relaunching process: Could not start '{info.ExecutablePath}'. Error: {ex.Message}\n");
            outputTextBox.AppendText("Check if the executable path is valid and you have permissions to run it.\n");
        }
    }

    /// <summary>
    /// Extracts the full path to the executable from the command line.
    /// </summary>
    private string ExtractExecutablePath(string fullCommandLine)
    {
        if (string.IsNullOrEmpty(fullCommandLine)) return string.Empty;

        // Check for quoted path (e.g., "C:\Program Files\App\App.exe" args...)
        if (fullCommandLine.TrimStart().StartsWith("\""))
        {
            int closingQuoteIndex = fullCommandLine.IndexOf('"', 1);
            if (closingQuoteIndex > 0)
            {
                // Return path without the quotes
                return fullCommandLine.Substring(1, closingQuoteIndex - 1);
            }
        }

        // Check for unquoted path (e.g., C:\Path\App.exe args...)
        int firstSpace = fullCommandLine.IndexOf(' ');

        // Return up to the first space, or the whole line if no space is found
        string path = firstSpace > 0 ? fullCommandLine.Substring(0, firstSpace) : fullCommandLine;

        // Simple check to ensure it ends with the target name (case-insensitive)
        if (path.EndsWith(TargetProcessName, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return string.Empty;
    }

    /// <summary>
    /// Isolates the value of the --datadir or /datadir argument.
    /// </summary>
    private string ParseDatadirArgument(string commandLine)
    {
        // Define common datadir argument tags
        string[] datadirTags = { "--datadir", "/datadir" };

        foreach (var tag in datadirTags)
        {
            // Case-insensitive search for the tag
            int tagIndex = commandLine.IndexOf(tag, StringComparison.OrdinalIgnoreCase);

            if (tagIndex != -1)
            {
                // Start looking immediately after the tag
                string remaining = commandLine.Substring(tagIndex + tag.Length).TrimStart();

                // If the next character is '=', skip it
                if (remaining.StartsWith("="))
                {
                    remaining = remaining.Substring(1).TrimStart();
                }

                // Check for quoted path
                if (remaining.StartsWith("\""))
                {
                    int secondQuote = remaining.IndexOf('"', 1);
                    if (secondQuote > 0)
                    {
                        // Path is everything between the first and second quote
                        return remaining.Substring(1, secondQuote - 1);
                    }
                }

                // Check for unquoted path (take up to the next space)
                int nextSpace = remaining.IndexOf(' ');
                return nextSpace > 0 ? remaining.Substring(0, nextSpace) : remaining;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts the last folder/directory name from a path string.
    /// </summary>
    private string GetLastPathComponent(string path)
    {
        if (string.IsNullOrEmpty(path)) return "Unknown";

        // Normalize and trim trailing separators
        path = path.TrimEnd('\\', '/');

        // Find the index of the last separator
        int lastSeparator = path.LastIndexOfAny(new char[] { '\\', '/' });

        // Return everything after the last separator
        return lastSeparator >= 0 ? path.Substring(lastSeparator + 1) : path;
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ProcessParameterFinderForm());
    }
}
