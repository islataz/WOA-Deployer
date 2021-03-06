using Iridio.Common;

namespace Deployer.Core.Deployers
{
    class CompileError : DeployError
    {
        public Errors Errors { get; }

        public CompileError(Errors errors)
        {
            Errors = errors;
        }

        public override string ToString()
        {
            return string.Join(", ", Errors);
        }
    }
}