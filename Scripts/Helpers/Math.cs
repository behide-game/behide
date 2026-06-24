namespace Behide.Helpers;

public static class Math
{
    public static int MathMod(int a, int b) =>
        (System.Math.Abs(a * b) + a) % b;
}
