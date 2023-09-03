using Market_Analizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Patern
{
    public static class PaternLogic
    {
        public static bool ifHammer(this List<Candle> list, int Number)
        {
            // проверяет, является ли свеча с номером Number молотом
            // out:
            //      true или false
            if (list.Count< Number)
            {
                return false;
            }
            decimal loShadow = Math.Min(list[Number].Open, list[Number].Close) - list[Number].Lo;
            decimal body = Math.Abs(list[Number].Open - list[Number].Close) == 0 ? 1 : Math.Abs(list[Number].Open - list[Number].Close);
            decimal hiShadow = list[Number].Hi - Math.Max(list[Number].Open, list[Number].Close);
            
            if (((loShadow >= body * 2) && (hiShadow < body)) || ((hiShadow >= body * 2) && (loShadow < body)))
            {
                return true;
            }
            return false;
        }

        public static int Trend(this List<Candle> list, int Number, int len)
        {
            // определяет направление тренда, начиная со свечи Number, глубиной просмотра len
            // out:
            // 1 - возрастающий тренд
            // 2 - нисходящий тренд
            // 0 - боковик
            if (Number + len > list.Count)
            {
                return 0;
            }
            if (list[Number+1+len].Hi > list[Number].Hi)
            {
                return 2;
            }
            return 0;
        }
    }
}
