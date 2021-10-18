namespace BasicSample.Calls
{
    public static class CountParams
    {
        public static int RunAsync(int arg1, params string[] args)
        {
            return args.Length + arg1;
        }
    }
}