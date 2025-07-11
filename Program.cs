using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;

public class TimeEntry
{
    public Employee Employee { get; set; } = new();
    public DateTime TimeIn { get; set; }
    public DateTime TimeOut { get; set; }
}

public class Employee
{
    public string Name { get; set; } = "";
}

public class EmployeeSummary
{
    public string Name { get; set; } = "";
    public double TotalHours { get; set; }
}

class Program
{
    static async Task Main()
    {
        List<TimeEntry> entries = new();

        try
        {
            string url = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUCgQ==";
            using HttpClient client = new();
            var response = await client.GetStringAsync(url);

            entries = JsonSerializer.Deserialize<List<TimeEntry>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();

            Console.WriteLine("✅ Data loaded from API.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ API failed: " + ex.Message);
            Console.WriteLine("⏳ Using dummy data instead...");

            entries = new List<TimeEntry>
            {
                new TimeEntry
                {
                    Employee = new Employee { Name = "Alice" },
                    TimeIn = new DateTime(2024, 1, 1, 9, 0, 0),
                    TimeOut = new DateTime(2024, 1, 1, 17, 0, 0)
                },
                new TimeEntry
                {
                    Employee = new Employee { Name = "Bob" },
                    TimeIn = new DateTime(2024, 1, 1, 10, 0, 0),
                    TimeOut = new DateTime(2024, 1, 1, 15, 0, 0)
                },
                new TimeEntry
                {
                    Employee = new Employee { Name = "Charlie" },
                    TimeIn = new DateTime(2024, 1, 1, 8, 0, 0),
                    TimeOut = new DateTime(2024, 1, 1, 20, 0, 0)
                },
                new TimeEntry
                {
                    Employee = new Employee { Name = "Short Worker" },
                    TimeIn = new DateTime(2024, 1, 1, 9, 0, 0),
                    TimeOut = new DateTime(2024, 1, 1, 10, 0, 0)
                }
            };
        }

        if (entries.Count == 0)
        {
            Console.WriteLine("❌ No data to process.");
            return;
        }

        var summary = entries
            .GroupBy(e => e.Employee.Name ?? "Unknown")
            .Select(g => new EmployeeSummary
            {
                Name = g.Key,
                TotalHours = g.Sum(e => (e.TimeOut - e.TimeIn).TotalHours)
            })
            .OrderByDescending(x => x.TotalHours)
            .ToList();

        GenerateHtmlReport(summary);

        Console.WriteLine("✅ report.html has been created.");

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "report.html",
            UseShellExecute = true
        });
    }

    static void GenerateHtmlReport(List<EmployeeSummary> employees)
    {
        string labels = string.Join(",", employees.Select(e => $"\"{e.Name}\""));
        string data = string.Join(",", employees.Select(e => e.TotalHours.ToString("F1")));

        string html = @$"<!DOCTYPE html>
<html>
<head>
    <title>Employee Time Report</title>
    <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
    <style>
        table {{ border-collapse: collapse; width: 60%; }}
        th, td {{ border: 1px solid #aaa; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .low-hours {{ background-color: #fdd; }}
    </style>
</head>
<body>
    <h2>Employee Total Time Worked</h2>
    <table>
        <tr><th>Name</th><th>Total Hours</th></tr>";

        foreach (var e in employees)
        {
            string rowClass = e.TotalHours < 100 ? " class='low-hours'" : "";
            html += $"<tr{rowClass}><td>{e.Name}</td><td>{e.TotalHours:F2}</td></tr>\n";
        }

        html += @$"
    </table>

    <h3>Work Distribution Pie Chart</h3>
    <canvas id='myChart' width='600' height='400'></canvas>
    <script>
        const ctx = document.getElementById('myChart').getContext('2d');
        new Chart(ctx, {{
            type: 'pie',
            data: {{
                labels: [{labels}],
                datasets: [{{
                    label: 'Total Hours',
                    data: [{data}],
                    backgroundColor: [
                        'rgba(255, 99, 132, 0.6)',
                        'rgba(54, 162, 235, 0.6)',
                        'rgba(255, 206, 86, 0.6)',
                        'rgba(75, 192, 192, 0.6)',
                        'rgba(153, 102, 255, 0.6)',
                        'rgba(255, 159, 64, 0.6)',
                        'rgba(100, 255, 218, 0.6)',
                        'rgba(255, 120, 255, 0.6)'
                    ],
                    borderWidth: 1
                }}]
            }},
            options: {{
                responsive: false
            }}
        }});
    </script>
</body>
</html>";

        File.WriteAllText("report.html", html);
        Console.WriteLine("✅ report.html created with Chart.js pie chart.");
    }
}
