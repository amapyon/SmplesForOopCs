﻿using System;
using System.IO;

namespace Step06
{
    public class Program
    {
        // 料金計算のための基礎情報
        private static readonly int INITIAL_BASIC_CHARGE = 1000;
        private static readonly int INITIAL_CALL_UNIT_PRICE = 20;
        private static readonly int FAMILY_SERVICE_BASIC_CHARGE = 100;
        private static readonly string FAMILY_SERVICE_CODE = "C1";
        private static readonly int DAY_SERVICE_BASIC_CHARGE = 200;
        private static readonly string DAY_SERVICE_CODE = "E1";

        // 各レコードの先頭文字 (Record Code)
        private static readonly string RC_OWNER_INFO = "1";
        private static readonly string RC_SERVICE_INFO = "2";
        private static readonly string RC_CALL_LOG = "5";
        private static readonly string RC_SEPARATOR = "9";

        // 各レコードの情報 (Record Information)
        private static readonly int RI_OF_OWNER_TEL_NUMBER = 2;
        private static readonly int RI_OF_SERVICE_CODE = 2;
        private static readonly int RI_SZ_SERVICE_CODE = 2;
        private static readonly int RI_OF_SERVICE_OPTION = 5;
        private static readonly int RI_OF_CALL_START_TIME = 13;
        private static readonly int RI_SZ_HOUR = 2;
        private static readonly int RI_OF_CALL_MINUTE = 19;
        private static readonly int RI_SZ_CALL_MINUTE = 3;
        private static readonly int RI_OF_CALL_NUMBER = 23;

        public static void Main(string[] args)
        {

            var fis = new FileStream("../../../record.log", FileMode.Open);
            var reader = new StreamReader(fis);

            var fos = new FileStream("../../../invoice.dat", FileMode.Create);
            var writer = new StreamWriter(fos);
            writer.NewLine = "\n";

            var invoice = new Invoice();
            var dayService = new DayService();
            var familyService = new FamilyService();

            string line = reader.ReadLine();
            while (line != null)
            {
                Console.WriteLine(line);

                if (line.StartsWith(RC_OWNER_INFO))
                {
                    // 契約者情報
                    invoice.SetOwnerTelNumber(line.Substring(RI_OF_OWNER_TEL_NUMBER));
                }
                else if (line.StartsWith(RC_SERVICE_INFO))
                {
                    // 加入サービス情報
                    Service(dayService, familyService, line);
                }
                else if (line.StartsWith(RC_CALL_LOG))
                {
                    // 通話記録
                    Call(invoice, dayService, familyService, line);
                }
                else if (line.StartsWith(RC_SEPARATOR))
                {
                    // 区切り
                    Separate(writer, invoice, dayService, familyService);
                }


                line = reader.ReadLine();
            }

            writer.Close();
            fos.Close();

            reader.Close();
            fis.Close();
        }

        private static void Service(DayService dayService, FamilyService familyService, string line)
        {
            if (DAY_SERVICE_CODE == line.Substring(RI_OF_SERVICE_CODE, RI_SZ_SERVICE_CODE))
            {
                // 昼トク割引
                dayService.Joined();
            }
            else if (FAMILY_SERVICE_CODE == line.Substring(RI_OF_SERVICE_CODE, RI_SZ_SERVICE_CODE))
            {
                // 家族割引 登録されている電話番号を一時保管
                familyService.AppendFamilyTelNumber(line.Substring(RI_OF_SERVICE_OPTION));
            }
        }

        private static void Call(Invoice invoice, DayService dayService, FamilyService familyService, string line)
        {
            // 単価を計算する
            int unitPrice = INITIAL_CALL_UNIT_PRICE;
            if (dayService.IsJoined())
            {
                int hour = int.Parse(line.Substring(RI_OF_CALL_START_TIME, RI_SZ_HOUR));
                if (dayService.IsServiceTime(hour))
                {
                    // 昼トク割引なら5円引き
                    unitPrice -= 5;
                }
            }

            if (familyService.IsFamilyTelNumber(line.Substring(RI_OF_CALL_NUMBER)))
            {
                // 家族割引なら半額
                unitPrice /= 2;
            }

            // 1通話あたりの通話料を計算し、全通話料に加算する
            string minutes = line.Substring(RI_OF_CALL_MINUTE, RI_SZ_CALL_MINUTE);
            invoice.AddCallCharge(unitPrice * int.Parse(minutes));
        }

        private static void Separate(StreamWriter writer, Invoice invoice, DayService dayService, FamilyService familyService)
        {

            // 基本料金の計算
            int basicCharge = INITIAL_BASIC_CHARGE;
            if (dayService.IsJoined())
            {
                basicCharge += DAY_SERVICE_BASIC_CHARGE;
            }
            if (familyService.IsJoined())
            {
                basicCharge += FAMILY_SERVICE_BASIC_CHARGE;
            }

            // 集計結果の出力
            writer.WriteLine("1 " + invoice.GetOwnerTelNumber());
            writer.WriteLine("5 " + basicCharge);
            writer.WriteLine("7 " + invoice.GetCallCharge());
            writer.WriteLine("9 ====================");

            // 変数の初期化
            invoice.Clear();
            dayService.Clear();
            familyService.Clear();
        }
    }

}
