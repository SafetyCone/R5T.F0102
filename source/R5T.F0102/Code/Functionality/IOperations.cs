using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using R5T.T0132;


namespace R5T.F0102
{
    [FunctionalityMarker]
    public partial interface IOperations : IFunctionalityMarker
    {
        /// <summary>
        /// For a project file, search its recursive project dependencies for Tailwind CSS content path files, and aggregate all globbed, relative content paths.
        /// </summary>
        public async Task GenerateAllTailwindContentPathsJsonFile(
            string projectFilePath)
        {
            var sourceProjectFilePaths = await this.GetTailwindSourceDependencyProjectFilePaths(projectFilePath);

            var allContentPaths = this.ProcessSourceProjectFilePaths(
                projectFilePath,
                sourceProjectFilePaths);

            var allTailwindContentPathsJsonFilePath = Instances.ProjectPathsOperator.GetTailwindAllContentPathsJsonFilePath(
                projectFilePath);

            Instances.JsonOperator.Serialize_Synchronous(
                allTailwindContentPathsJsonFilePath,
                allContentPaths);
        }

        /// <summary>
        /// Search through all recursive dependency projects of the main project, determining which projects have a Tailwind contents paths JSON file.
        /// </summary>
        public async Task<string[]> GetTailwindSourceDependencyProjectFilePaths(string projectFilePath)
        {
            var allRecursiveDependencyProjectFilePaths = await Instances.ProjectReferencesOperator.GetAllRecursiveProjectReferences_Inclusive(
                projectFilePath);

            var sourceProjectFilePaths = allRecursiveDependencyProjectFilePaths
                .Where(projectFilePath =>
                {
                    var contentPathsFilePath = Instances.ProjectPathsOperator.GetTailwindContentPathsJsonFilePath(projectFilePath);

                    var output = Instances.FileSystemOperator.Exists_File(contentPathsFilePath);
                    return output;
                })
                .OrderAlphabetically_OnlyIfDebug()
                .Now();

            return sourceProjectFilePaths;
        }

        /// <summary>
        /// Get the list of all destination project relative content paths, for a set of source project file paths.
        /// </summary>
        public string[] ProcessSourceProjectFilePaths(
            string destinationProjectFilePath,
            IEnumerable<string> sourceProjectFilePaths)
        {
            var output = sourceProjectFilePaths
                .SelectMany(sourceProjectFilePath => this.GetSourceRelativeGlobbedPaths(
                    destinationProjectFilePath,
                    sourceProjectFilePath))
                .Transform(this.FormatGlobbedPathsForTailwindCss)
                .Now();

            return output;
        }

        /// <summary>
        /// For a source project file path, load the globbed content paths (which are source project-directory relative), convert to rooted paths using the source project directory path, then convert the paths to be destination project-relative using the destination project file path.
        /// </summary>
        public IEnumerable<string> GetSourceRelativeGlobbedPaths(
            string destinationProjectFilePath,
            string sourceProjectFilePath)
        {
            var contentPathsFilePath = Instances.ProjectPathsOperator.GetTailwindContentPathsJsonFilePath(sourceProjectFilePath);

            var globbedRelativePaths = Instances.JsonOperator.Deserialize_Synchronous<string[]>(contentPathsFilePath);

            var sourceProjectDirectoryPath = Instances.ProjectPathsOperator.GetProjectDirectoryPath(sourceProjectFilePath);

            var sourceRelativeGlobbedPaths = globbedRelativePaths
                .Select(globbedRelativePath => Instances.PathOperator.Combine(
                    sourceProjectDirectoryPath,
                    globbedRelativePath))
                .Select(absoluteGlobbedPath => Instances.PathOperator.Get_RelativePath(
                    destinationProjectFilePath,
                    absoluteGlobbedPath))
                ;

            return sourceRelativeGlobbedPaths;
        }

        /// <summary>
        /// Format globbed content paths the way Tailwind CSS wants them:
        /// * Use the non-Windows directory separator.
        /// * Start the path with the current-directory relative path indicator ("./").
        /// </summary>
        public IEnumerable<string> FormatGlobbedPathsForTailwindCss(IEnumerable<string> globbedPaths)
        {
            var formattedGlobbedPaths = globbedPaths
                // Tailwind CSS wants non-Windows paths.
                .Select(path => Instances.PathOperator.Ensure_UsesNonWindowsDirectorySeparator(path))
                // Tailwind CSS conventions is to start from the current directory with "./".
                .Select(path => $"./{path}")
                ;

            return formattedGlobbedPaths;
        }
    }
}
