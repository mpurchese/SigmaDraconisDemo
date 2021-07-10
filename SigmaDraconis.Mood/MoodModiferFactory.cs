namespace SigmaDraconis.Mood
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WorldInterfaces;

    public static class MoodModiferFactory
    {
        public static List<IMoodModifer> GetAll()
        {
            // Get current assembly
            var assembly = typeof(MoodModiferFactory).Assembly;

            // Get all types that implement IMoodModifer
            var modifierTypes = assembly.GetTypes().Where(p => typeof(IMoodModifer).IsAssignableFrom(p));

            // Return a list with an instance of each type
            return modifierTypes.Select(t => (IMoodModifer)Activator.CreateInstance(t)).ToList();
        }
    }
}
