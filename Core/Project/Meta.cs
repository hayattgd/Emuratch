namespace Emuratch.Core.Project;

public class Meta
{
    public struct Platform
    {
        public Platform(string name, string url)
        {
            this.name = name;
            this.url = url;
        }

        public string name = "None";
        public string url = "";
    }

    public string semver = "3.0.0";
    public string vm = "0.0.0";
    public string agent = "";
    public Platform platform;
}