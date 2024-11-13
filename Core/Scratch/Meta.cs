namespace Emuratch.Core.Scratch;

public class Meta
{
    public struct Platform
    {
        public Platform(string name, string url)
        {
            this.name = name;
            this.url = url;
        }

        public string name = "Scratch";
        public string url = "https://scratch.mit.edu";
    }

    public string semver = "3.0.0";
    public string vm = "0.0.0";
    public string agent = "";
    public Platform platform;
}