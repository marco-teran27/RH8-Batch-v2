using Interfaces;

namespace Commons.Params
{
    public class ProjectName
    {
        private static readonly ProjectName _instance = new ProjectName();
        private string _name = string.Empty;

        private ProjectName() { }

        public static ProjectName Instance => _instance;

        public void SetName(IConfigDataResults config)
        {
            _name = config.ProjectName;
        }

        public string Name => _name;
    }
}