namespace BasicSample.Calls
{
    public class CountParams
    {
        public static int RunAsync(int arg1, params string[] args)
        {
            return args.Length + arg1;
        }
    }
}