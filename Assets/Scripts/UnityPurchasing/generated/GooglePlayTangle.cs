// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("+fejgpYXnqSv9+CTSCzR0X6eVldTomurVUTACNzEcB+T9ruNuk9BUt26RBexNqeZ/mFJ7j98/oha7b6nL19fR7OrALUebnlh+VeWln9ruDJk5+nm1mTn7ORk5+fmdoQiRaBMqllTnnuqx1Bh6QWGOXFugPdBGjIk1mTnxNbr4O/MYK5gEevn5+fj5uXJVELRlBD+3C6WtA8YBV4OK2EiPbEOejkOH5KF6yQ3GQpL3Qj8QG2oGmVaQ6txcFgH6B7VaLJT0VJ9u5ehIQtIEW71/5nO/pbu4iK8LEtKH3iPd9E7WqSmfd/PQv/qbyuPgwSYvyTFjbPokfmQTZkM5RAZXvnZjVESdHLilnXWQSKfZBqTpr5hu+bb+Yxmiv9iHnH8deTl5+bn");
        private static int[] order = new int[] { 9,4,10,3,4,6,9,11,9,12,12,12,12,13,14 };
        private static int key = 230;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
