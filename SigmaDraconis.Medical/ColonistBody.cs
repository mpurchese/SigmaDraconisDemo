namespace SigmaDraconis.Medical
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;

    [ProtoContract]
    public class ColonistBody
    {
        [ProtoMember(1)]
        public double Hydration { get; set; }

        [ProtoMember(2)]
        public double Nourishment { get; set; }

        [ProtoMember(3)]
        public double Temperature { get; set; }

        [ProtoMember(4)]
        public double TemperatureForecast { get; set; }

        [ProtoMember(5)]
        public double HungerRate { get; private set; }

        [ProtoMember(6)]
        public double ThirstRate { get; private set; }

        [ProtoMember(7, IsRequired = true)]
        public bool IsDead { get; private set; }

        [ProtoMember(8)]
        public DeathReason DeathReason { get; private set; }

        [ProtoMember(9)]
        public bool IsSleeping { get; set; }

        [ProtoMember(10)]
        public int SleepTimer { get; set; }

        [ProtoMember(11)]
        public double Energy { get; set; }

        [ProtoMember(12)]
        public Dictionary<MedicalConditionType, MedicalCondition> MedicalConditions;

        [ProtoMember(13)]
        public List<MedicalCondition> Infections;

        private bool isDrinking;
        private bool isEating;

        public double StarvationLevel => this.MedicalConditions?.ContainsKey(MedicalConditionType.Starvation) == true ? this.MedicalConditions[MedicalConditionType.Starvation].Severity : 0;
        public double DehydrationLevel => this.MedicalConditions?.ContainsKey(MedicalConditionType.Dehydration) == true ? this.MedicalConditions[MedicalConditionType.Dehydration].Severity : 0;

        public void Update(float worldTemperature, int hungerRateMultiplier, int thirstRateMultiplier)
        {
            if (this.MedicalConditions == null) this.MedicalConditions = new Dictionary<MedicalConditionType, MedicalCondition>();
            if (this.Infections == null) this.Infections = new List<MedicalCondition>();

            this.UpdateBodyTemperature(worldTemperature);
            if (!this.IsDead) this.UpdateHungerAndThirst(hungerRateMultiplier, thirstRateMultiplier);
        }

        public void Eat(double nourishment)
        {
            this.Nourishment += nourishment;
            this.HungerRate = -nourishment;
            this.isEating = true;
        }

        public void Drink(double hydration)
        {
            this.Hydration += hydration;
            this.ThirstRate = -hydration;
            this.isDrinking = true;
        }

        private void UpdateHungerAndThirst(int hungerRateMultiplier, int thirstRateMultiplier)
        {
            if (this.isDrinking) this.isDrinking = false;
            else this.ThirstRate = 0.01 * thirstRateMultiplier * (this.IsSleeping ? Constants.ColonistSleepingThirstRate : Constants.ColonistThirstRate);

            if (this.isEating) this.isEating = false;
            else this.HungerRate = 0.01 * hungerRateMultiplier * (this.IsSleeping ? Constants.ColonistSleepingHungerRate : Constants.ColonistHungerRate);

            this.Nourishment -= this.HungerRate;
            if (this.Nourishment > 0.01 && this.MedicalConditions.ContainsKey(MedicalConditionType.Starvation))
            {
                // Malnourished but has had food - malnourishment decreases
                this.Nourishment -= Constants.ColonistHungerRate;
                this.HungerRate += Constants.ColonistHungerRate;
                this.MedicalConditions[MedicalConditionType.Starvation].Severity -= Constants.ColonistHungerRate;
                if (this.MedicalConditions[MedicalConditionType.Starvation].Severity <= 0) this.MedicalConditions.Remove(MedicalConditionType.Starvation);
            }
            else if (this.Nourishment < 0)
            {
                // No food - malnourishment increases
                if (!this.MedicalConditions.ContainsKey(MedicalConditionType.Starvation))
                {
                    this.MedicalConditions.Add(MedicalConditionType.Starvation, new MedicalCondition(MedicalConditionType.Starvation));
                }

                this.MedicalConditions[MedicalConditionType.Starvation].Severity -= this.Nourishment * 2.0;
                if (this.MedicalConditions[MedicalConditionType.Starvation].Severity >= 100.0)
                {
                    this.IsDead = true;  // Died!!!
                    this.DeathReason = DeathReason.Hunger;
                }

                this.Nourishment = 0;
            }

            this.Hydration -= this.ThirstRate;
            if (this.Hydration > 0.01 && this.MedicalConditions.ContainsKey(MedicalConditionType.Dehydration))
            {
                // Dehydrated but has had water - dehydration decreases
                this.Hydration -= Constants.ColonistThirstRate;
                this.ThirstRate += Constants.ColonistThirstRate;
                this.MedicalConditions[MedicalConditionType.Dehydration].Severity -= Constants.ColonistThirstRate;
                if (this.MedicalConditions[MedicalConditionType.Dehydration].Severity <= 0) this.MedicalConditions.Remove(MedicalConditionType.Dehydration);
            }
            else if (this.Hydration < 0)
            {
                // No water - dehydration increases
                if (!this.MedicalConditions.ContainsKey(MedicalConditionType.Dehydration))
                {
                    this.MedicalConditions.Add(MedicalConditionType.Dehydration, new MedicalCondition(MedicalConditionType.Dehydration));
                }

                this.MedicalConditions[MedicalConditionType.Dehydration].Severity -= this.Hydration * 2.0;
                if (this.MedicalConditions[MedicalConditionType.Dehydration].Severity >= 100.0)
                {
                    this.IsDead = true;  // Died!!!
                    this.DeathReason = DeathReason.Thirst;
                }

                this.Hydration = 0;
            }
        }

        private void UpdateBodyTemperature(float worldTemperature)
        {
            var tempDiff = worldTemperature - this.Temperature;
            var prevTemperature = this.Temperature;
            this.Temperature += tempDiff * (tempDiff > 0 ? 0.001 : 0.0005);  // Heats faster than cools

            if (this.Temperature > 20.01) this.Temperature -= 0.01f;
            else if (this.Temperature < 19.99) this.Temperature += 0.01f;

            if (this.Temperature > 10.0 && this.MedicalConditions.ContainsKey(MedicalConditionType.Hypothermia))
            {
                // Hypothermic but warming up
                this.MedicalConditions[MedicalConditionType.Hypothermia].Severity -= 2.0 * (this.Temperature - 10.0);
                this.Temperature = 10.0;
                if (this.MedicalConditions[MedicalConditionType.Hypothermia].Severity <= 0) this.MedicalConditions.Remove(MedicalConditionType.Hypothermia);
            }
            else if (this.Temperature < 10.0)
            {
                // Hypothermia
                if (!this.MedicalConditions.ContainsKey(MedicalConditionType.Hypothermia))
                {
                    this.MedicalConditions.Add(MedicalConditionType.Hypothermia, new MedicalCondition(MedicalConditionType.Hypothermia));
                }

                this.MedicalConditions[MedicalConditionType.Hypothermia].Severity += 8.0 * (10.0 - this.Temperature);
                if (this.MedicalConditions[MedicalConditionType.Hypothermia].Severity >= 100.0)
                {
                    this.IsDead = true;  // Died!!!
                    this.DeathReason = DeathReason.Cold;
                }

                this.Temperature = 10.0;
            }
            else if (this.Temperature < 30.0 && this.MedicalConditions.ContainsKey(MedicalConditionType.Hyperthermia))
            {
                // Hyperthermic but coolong down
                this.MedicalConditions[MedicalConditionType.Hyperthermia].Severity -= 10.0 * (30.0 - this.Temperature);
                this.Temperature = 30.0;
                if (this.MedicalConditions[MedicalConditionType.Hyperthermia].Severity <= 0) this.MedicalConditions.Remove(MedicalConditionType.Hyperthermia);
            }
            else if (this.Temperature > 30.0)
            {
                // Hypothermia
                if (!this.MedicalConditions.ContainsKey(MedicalConditionType.Hyperthermia))
                {
                    this.MedicalConditions.Add(MedicalConditionType.Hyperthermia, new MedicalCondition(MedicalConditionType.Hyperthermia));
                }

                this.MedicalConditions[MedicalConditionType.Hyperthermia].Severity += 10.0 * (this.Temperature - 30.0);
                if (this.MedicalConditions[MedicalConditionType.Hyperthermia].Severity >= 100.0)
                {
                    this.IsDead = true;  // Died!!!
                    this.DeathReason = DeathReason.Heat;
                }

                this.Temperature = 30.0;
            }

            // Roughly predict temperature a few seconds ahead
            if (this.Temperature < 19.9) this.TemperatureForecast = Math.Min(this.Temperature + (300 * (this.Temperature - prevTemperature)), 20.0);
            else if (this.Temperature > 20.1) this.TemperatureForecast = Math.Max(this.Temperature + (300 * (this.Temperature - prevTemperature)), 20.0);
            else this.TemperatureForecast = this.Temperature;
        }
    }
}
