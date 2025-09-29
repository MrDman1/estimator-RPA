using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Nuform.App.Pages
{
    public partial class AgentPage : Page
    {
        // …

        private void DataEntryWithSof_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("SOF-driven data entry will be available in the next commit (mapping + file picker).");
        }

        private void SofGeneratorDataEntry_Click(object sender, RoutedEventArgs e)
        {
            // You already have the calculator page; reuse it:
            var window = Window.GetWindow(this) as MainWindow;
            if (window != null) window.MainFrame.Navigate(new IntakePage());
        }

        private void EstimateGeneration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1) Prepare a sample job payload. Replace with real values from your intake form.
                var job = new
                {
                    maximizing = new {
                        company = "Modern Building Systems",
                        contact = "Jim Dillard",
                        objective = "Star Stop 148 - Car Wash",
                        estimateNumber = "25681",
                        salesPerson = "Wayne Smith",
                        estimator = "Damian Beland",
                        opportunitySource = "Sales Agent",
                        currency = "USD",
                        shipToMode = "Address",
                        shipToCity = "Spring",
                        shipToProvince = "TX",
                        discounts = new {
                            Conform = 0, Reline = 0, Renu = 0,
                            Specialty = 0, OtherNonPvc = 0, Shipping = 1100
                        }
                    },
                    nsd = new {
                        materialTypes = new[] { "RELINE" },   // RL, CF2, CF4, etc.
                        signedEstimate = false,
                        sofPath = ""                          // if you already have a PDF to upload
                    },
                    trello = new { enabled = false }
                };

                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string scriptPath = Path.Combine(appDir, "automation_node", "create_estimate.js");
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show($"Automation script not found:\n{scriptPath}");
                    return;
                }

                // 2) Serialize job to a temp file
                string tempJson = Path.Combine(Path.GetTempPath(), $"nuform_job_{Guid.NewGuid():N}.json");
                File.WriteAllText(tempJson, JsonSerializer.Serialize(job));

                // 3) Spawn Node + Playwright (headed)
                var psi = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"\"{scriptPath}\" \"{tempJson}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = appDir
                };

                var p = Process.Start(psi);
                p.OutputDataReceived += (_, a) => { if (a.Data != null) Debug.WriteLine(a.Data); };
                p.ErrorDataReceived +=  (_, a) => { if (a.Data != null) Debug.WriteLine(a.Data); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                MessageBox.Show("Automation launched. Switch to the headed browser to complete any logins.\nThe job payload is a placeholder—wire it to your form next.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start automation: " + ex.Message);
            }
        }
    }
}
