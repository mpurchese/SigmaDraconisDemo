namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Generic;
    using WorldInterfaces;

    public static class ResourceDeconstructionController
    {
        public static Dictionary<int, ResourceDeconstructionJob> Jobs = new Dictionary<int, ResourceDeconstructionJob>();

        public static void Clear()
        {
            Jobs.Clear();
        }

        public static int Add(IColonist colonist, IRecyclableThing target, int frames)
        {
            var job = new ResourceDeconstructionJob(colonist, target, frames);
            job.Start();
            Jobs.Add(job.ID, job);
            return job.ID;
        }

        public static bool IsFinished(int jobId)
        {
            return Jobs[jobId].IsFinished;
        }

        public static void Update()
        {
            foreach (var job in Jobs)
            {
                if (!job.Value.IsFinished) job.Value.Update();
            }
        }
    }
}
