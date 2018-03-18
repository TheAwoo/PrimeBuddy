using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace PrimeBuddy
{
    class BuddySheets
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "Prime Buddy";
        const int ITEM_AMOUNT = 224;

        UserCredential Credentials;
        SheetsService Service;
        string SheetID;
        int StartingRow, StartingColumn;
        string ItemRange, SheetName;

        public BuddySheets(string sheetID, string sheetName, int columnStart, int rowStart)
        {
            this.SheetID = sheetID;
            this.StartingRow = rowStart;
            this.StartingColumn = columnStart;
            this.SheetName = sheetName;
            // 'Prime Stuff (flat)'!A3:B226
            this.ItemRange = string.Format("'{0}'!{1}:{2}", sheetName, A1(columnStart, rowStart), A1(columnStart + 1, rowStart + ITEM_AMOUNT));

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                path = Path.Combine(path, ".buddy/creds.json");

                ClientSecrets secrets = GoogleClientSecrets.Load(stream).Secrets;
                FileDataStore store = new FileDataStore(path, true);

                Credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, Scopes, "BuddyUser", CancellationToken.None, store).Result;
            }

            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credentials,
                ApplicationName = ApplicationName,
            });
        }

        public List<SheetItem> GetInventory()
        {
            SpreadsheetsResource.ValuesResource.GetRequest request = Service.Spreadsheets.Values.Get(SheetID, ItemRange);
            ValueRange response = request.Execute();

            List<SheetItem> ret = new List<SheetItem>();

            foreach (var i in response.Values)
                ret.Add(new SheetItem() { Name = (string) i[0], Amount = (string) i[1], });

            return ret;
        }

        public bool UpdateItemCount(string item, int delta = 1)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request = Service.Spreadsheets.Values.Get(SheetID, ItemRange);
            ValueRange response = request.Execute();

            int rownum = this.StartingRow;

            foreach (var row in response.Values)
            {
                if (item.Equals((string) row[0], System.StringComparison.CurrentCultureIgnoreCase))
                {
                    int currentamount = int.Parse((string) row[1]);
                    currentamount += delta;

                    string updateCell = string.Format("{0}!{1}", SheetName, A1(StartingColumn + 1, rownum));
                    ValueRange updateRange = new ValueRange();
                    updateRange.MajorDimension = "COLUMNS";
                    var temp = new List<object> { currentamount };
                    updateRange.Values = new List<IList<object>> { temp };

                    // WHY IS JAVA LEAKING INTO MY C#
                    SpreadsheetsResource.ValuesResource.UpdateRequest updateRequest = Service.Spreadsheets.Values.Update(updateRange, SheetID, updateCell);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                    var updateresponse = updateRequest.Execute();

                    return updateresponse.UpdatedCells > 0;
                }

                rownum++;
            }

            return false;
        }

        static string A1(int column, int row)
        {
            string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            string ret = "";

            while (column >= 0)
            {
                int c = column % 26;

                ret += letters[c];
                column -= 26;
            }

            ret += row.ToString();
            return ret;
        }
    }

    public class SheetItem
    {
        public string Name { get; set; }
        public string Amount { get; set; }
    }
}
