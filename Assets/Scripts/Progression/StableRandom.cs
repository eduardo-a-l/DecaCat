public struct StableRandom
{
    private uint state;

    public StableRandom(int seed, int salt)
    {
        unchecked
        {
            uint value = (uint)seed * 2654435761u + (uint)salt * 40503u + 0x9E3779B9u;

            value ^= value >> 15;
            value *= 2246822519u;
            value ^= value >> 13;
            value *= 3266489917u;
            value ^= value >> 16;

            state = value != 0 ? value : 0x1234567u;
        }
    }

    public uint NextUInt()
    {
        uint value = state;

        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;

        state = value;

        return value;
    }

    public int Next(int maxExclusive)
    {
        return (int)(NextUInt() % (uint)maxExclusive);
    }
}
