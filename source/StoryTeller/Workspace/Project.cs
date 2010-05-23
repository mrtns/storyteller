using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using FubuCore;
using FubuCore.Util;
using StoryTeller.Domain;
using StoryTeller.Engine;
using StoryTeller.Engine.Sets;
using StoryTeller.Persistence;
using System.Linq;

namespace StoryTeller.Workspace
{
    public class Project : IProject
    {
        private string _fileName;
        private string _projectFolder;
        private int _timeoutInSeconds;
        private readonly Cache<string, WorkspaceFilter> _workspaces = new Cache<string, WorkspaceFilter>(name => new WorkspaceFilter()
        {
            Name = name
        });

        private IEnumerable<WorkspaceFilter> _selectedWorkspaces = new WorkspaceFilter[0];

        public Project()
        {
            TimeoutInSeconds = 5;
        }

        public Project(string filename)
            : this()
        {
            FileName = filename;
        }

        public WorkspaceFilter WorkspaceFor(string workspaceName)
        {
            return _workspaces[workspaceName];
        }

        public WorkspaceFilter[] Workspaces
        {
            get
            {
                return _workspaces.GetAll();
            }
            set
            {
                value.Each(w =>
                {
                    _workspaces[w.Name] = w;
                });
            }
        }


        public string FixtureAssembly { get; set; }
        public string BinaryFolder { get; set; }

        public string TestFolder { get; set; }

        [XmlIgnore]
        public string ProjectFolder
        {
            get
            {
                if (_projectFolder.IsEmpty()) return Path.GetFullPath(".");

                return _projectFolder;
            }
            set
            {
                _projectFolder = value;
                if (!Path.IsPathRooted(_projectFolder) && _projectFolder.IsNotEmpty())
                {
                    _projectFolder = Path.GetFullPath(_projectFolder);
                }

                if (_projectFolder.IsEmpty())
                {
                    _projectFolder = Path.GetFullPath(".");
                }

            }
        }

        #region IProject Members

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                _projectFolder = Path.GetDirectoryName(_fileName);
            }
        }

        public int TimeoutInSeconds { get { return _timeoutInSeconds > 0 ? _timeoutInSeconds : 5; } set { _timeoutInSeconds = value; } }

        public string SystemTypeName { get; set; }
        public string Name { get; set; }

        public string ConfigurationFileName { get; set; }


        public string GetBinaryFolder()
        {
            return getCorrectPath(BinaryFolder);
        }

        public Hierarchy LoadTests()
        {
            var hierarchy = new Hierarchy(this);

            new HierarchyLoader(GetTestFolder(), hierarchy, this).Load();

            return hierarchy;
        }

        public void Save(Test test)
        {
            string path = GetTestPath(test);
            new TestWriter().WriteToFile(test, path);
        }

        public void DeleteFile(Test test)
        {
            string path = GetTestPath(test);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void RenameTest(Test test, string name)
        {
            DeleteFile(test);
            test.Name = name;
            Save(test);
        }

        public ITestRunner LocalRunner()
        {
            Type type = Type.GetType(SystemTypeName);
            var runner = Activator.CreateInstance(type).As<ITestRunner>();

            return runner;
        }

        public void CreateDirectory(Suite suite)
        {
            string path = Path.Combine(GetTestFolder(), suite.GetFolder());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        #endregion

        public static Project LoadFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }

            var fileSystem = new FileSystem();
            var project = fileSystem.LoadFromFile<Project>(filename);
            project.ProjectFolder = Path.GetDirectoryName(filename);
            project.FileName = filename;

            return project;
        }

        public string GetTestFolder()
        {
            return getCorrectPath(TestFolder);
        }

        public string GetBaseProjectFolder()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                return string.Empty;
            }

            return Path.GetDirectoryName(_fileName);
        }

        private string getCorrectPath(string folder)
        {

            if (folder.IsEmpty()) return string.Empty;

            if (Path.IsPathRooted(folder))
            {
                return folder;
            }


            string projectFolder = Path.IsPathRooted(ProjectFolder)
                                       ? ProjectFolder
                                       : Path.GetFullPath(ProjectFolder);


            string path = Path.Combine(projectFolder, folder);
            return Path.GetFullPath(path);
        }

        public void Save(string filename)
        {
            FileName = filename;
            new FileSystem().PersistToFile(this, filename);
        }

        public string GetTestPath(Test test)
        {
            string fileName = test.FileName;
            return Path.Combine(GetTestFolder(), fileName);
        }

        #region Nested type: HierarchyLoader

        public class HierarchyLoader
        {
            private readonly Hierarchy _hierarchy;
            private readonly IProject _project;
            private readonly ITestReader _reader = new TestReader();
            private readonly FileSystem _system = new FileSystem();
            private readonly string _topFolder;

            public HierarchyLoader(string topFolder, Hierarchy hierarchy, IProject project)
            {
                _topFolder = topFolder;
                _hierarchy = hierarchy;
                _project = project;
            }

            public void Load()
            {
                loadTestsInFolder(_topFolder, _hierarchy);
            }

            private void loadTestsInFolder(string folder, Suite parent)
            {
                foreach (string file in _system.GetFiles(folder, "xml"))
                {
                    Test test = _reader.ReadFromFile(file);
                    parent.AddTest(test);
                }

                // load the tests from the sub folders
                foreach (string subFolder in _system.GetSubFolders(folder))
                {
                    string name = Path.GetFileName(subFolder);
                    var child = parent is Hierarchy ? new WorkspaceSuite(name){Filter = _project.WorkspaceFor(name)} : new Suite(name);
                    parent.AddSuite(child);

                    loadTestsInFolder(subFolder, child);
                }
            }
        }

        #endregion

        public WorkspaceFilter CurrentFixtureFilter()
        {
            return new WorkspaceFilter(_selectedWorkspaces);
        }

        // TODO -- this needs to be called on project loading
        public void SelectWorkspaces(IEnumerable<string > workspaceNames)
        {
            _selectedWorkspaces = workspaceNames.Select(x => WorkspaceFor(x));
        }

        public IEnumerable<WorkspaceFilter> SelectedWorkspaces
        {
            get { return _selectedWorkspaces; }
        }
    }
}