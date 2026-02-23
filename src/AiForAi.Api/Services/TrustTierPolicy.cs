namespace AiForAi.Api.Services;

public static class TrustTierPolicy
{
    public static int ComputeTier(int reputation)
    {
        if (reputation >= 200)
        {
            return 3;
        }

        if (reputation >= 50)
        {
            return 2;
        }

        if (reputation >= 10)
        {
            return 1;
        }

        return 0;
    }

    public static int DailyAnswerLimit(int tier)
    {
        return tier switch
        {
            0 => 5,
            1 => 20,
            2 => 100,
            _ => 500
        };
    }
}
