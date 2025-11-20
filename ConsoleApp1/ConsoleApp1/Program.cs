using System;
using System.IO;
using System.Text;
using ClosedXML.Excel;

namespace THSR_Timetable_Excel2Sql
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // TODO: 改成你自己的 Excel 路徑
            string excelPath = @"C:\Users\micha\Downloads\THSR_timetable_full (1).xlsx";

            // 輸出的 SQL 檔案
            string outputSqlPath = @"C:\Users\micha\Downloads\TimetableRaw_Insert.sql";

            if (!File.Exists(excelPath))
            {
                Console.WriteLine("找不到 Excel 檔案：" + excelPath);
                return;
            }

            var sb = new StringBuilder();

            using (var wb = new XLWorkbook(excelPath))
            {
                var ws = wb.Worksheet(1); // 第一個工作表

                int row = 2;  // 第1列是標題，從第2列開始讀
                while (true)
                {
                    var trainNoCell = ws.Cell(row, 1);
                    if (trainNoCell.IsEmpty())
                        break; // 遇到空白列就結束

                    string trainNoStr = trainNoCell.GetValue<string>().Trim();
                    if (string.IsNullOrWhiteSpace(trainNoStr))
                        break;

                    int trainNo = int.Parse(trainNoStr);

                    string direction = ws.Cell(row, 2).GetValue<string>().Trim(); // 南下 / 北上
                    string depTime = ws.Cell(row, 3).GetValue<string>().Trim(); // 06:25
                    string arrTime = ws.Cell(row, 4).GetValue<string>().Trim(); // 07:30

                    // 星期欄位（● 或 空白）
                    string mon = ws.Cell(row, 5).GetValue<string>().Trim();
                    string tue = ws.Cell(row, 6).GetValue<string>().Trim();
                    string wed = ws.Cell(row, 7).GetValue<string>().Trim();
                    string thu = ws.Cell(row, 8).GetValue<string>().Trim();
                    string fri = ws.Cell(row, 9).GetValue<string>().Trim();
                    string sat = ws.Cell(row, 10).GetValue<string>().Trim();
                    string sun = ws.Cell(row, 11).GetValue<string>().Trim();

                    // 為了避免中文或特殊字元出問題，字串一律用 N'...'
                    string sql = $@"
INSERT INTO TimetableRaw
    (TrainNo, Direction, DepartureTime, ArrivalTime, Mon, Tue, Wed, Thu, Fri, Sat, Sun)
VALUES
    ({trainNo},
     N'{direction.Replace("'", "''")}',
     '{depTime}',
     '{arrTime}',
     N'{mon.Replace("'", "''")}',
     N'{tue.Replace("'", "''")}',
     N'{wed.Replace("'", "''")}',
     N'{thu.Replace("'", "''")}',
     N'{fri.Replace("'", "''")}',
     N'{sat.Replace("'", "''")}',
     N'{sun.Replace("'", "''")}');";

                    sb.AppendLine(sql);
                    row++;
                }
            }

            File.WriteAllText(outputSqlPath, sb.ToString(), Encoding.UTF8);

            Console.WriteLine("已產生 SQL 檔：");
            Console.WriteLine(outputSqlPath);
            Console.WriteLine("按任意鍵結束...");
            Console.ReadKey();
        }
    }
}
