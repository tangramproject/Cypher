namespace TangramCypher.ApplicationLayer.Actor
{
    public class TokenDto
    {
        public EnvelopeDto Envelope { get; set; }

        public string Hint { get; set; }

        public string Keeper { get; set; }

        public int Version { get; set; }

        public string Principle { get; set; }

        public string Stamp { get; set; }
    }
}
