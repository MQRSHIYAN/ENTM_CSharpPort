﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments.SeasonTask;
using ENTM.Replay;
using ENTM.Utility;

namespace ENTM.Experiments.SeasonTask
{
    class TwoStepSeasonTaskEnviroment : SeasonTaskEnvironment
    {
       
        public TwoStepSeasonTaskEnviroment(SeasonTaskProperties props) : base(props)
        {

        }

        public override int TotalTimeSteps => Sequence.Length * 2 + 1; // we have one extra scoring step at the end for the last food eaten

        public override double[] PerformAction(double[] action)
        {
            Debug.LogHeader("SEASON TASK START", true);
            Debug.Log($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.Log($"{"Step:",-16} {_step}", true);
            double thisScore = 0;
            double[] observation = new double[0];
            int task = _step % 2; // 0 = reward / food step, 1 = eat step
            switch (task)
            {
                case 0:
                    Debug.Log("Task: Reward / Food Step", true);
                    double eatVal = action[0];
                    thisScore = Evaluate(eatVal, (_step / 2) - 1);
                    observation = GetOutput(_step, thisScore);
                    if (!IsFirstDayOfSeasonInFirstYear((_step/2) - 1))
                    {
                        _score += thisScore;
                    }
                    else
                    {
                        _score = _score;
                    }
                    Debug.Log($"{"Eating:",-16} {eatVal}" +
                                $"\n{"Last Was Poisonous:",-16} {Sequence[_step - 1].IsPoisonous}" +
                                $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                                $"\n{"Total Score:",-16} {_score.ToString("F4")} / {_step - 1}" +
                                $"\n{"Max Score:",-16} {Sequence.Length.ToString("F4")}", true);
                    break;
                case 1:
                    Debug.Log("Task: Eat Step", true);
                    observation = GetOutput(_step, -1);
                    break;
                default:
                    break;
            }

            Debug.LogHeader("SEASON TASK END", true);

            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, observation, thisScore);
            }

            _step++;
            return observation;
        }

        protected override double[] GetOutput(int step, double evaluation)
        {
            double[] observation = new double[OutputCount];
            int task = step % 2; // 0 = reward / food step, 1 = eat step
            switch (task)
            {
                case 0:
                    if (step != Sequence.Length * 2) // the last step is scoring only
                    {
                        Food currentFood = Sequence[step/2];
                        observation[currentFood.Type] = 1; // return the current food
                    }
                    if (step != 0) // first step has food only
                    {
                        if (evaluation == 0) // return evaluation from last food
                            observation[observation.Length - 1] = 1;
                        else
                            observation[observation.Length - 2] = 1;
                    }
                    break;
                case 1:
                    break;
                default:
                    break;
            }
            return observation;
        }
    }
}
