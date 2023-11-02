namespace MacTweaks.Modules
{
    public interface IModule
    {
        public static virtual string ModuleIdentifier { get; } = null;
        
        void Start();
        
        void Stop();
    }
}