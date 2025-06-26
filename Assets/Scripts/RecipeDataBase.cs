using UnityEngine;
using System.Collections.Generic;

namespace GameCore
{
    [System.Serializable]
    public class Ingredient
    {
        public CropType cropType;
        public int requiredAmount;
        public int currentAmount;

        public bool IsComplete()
        {
            return currentAmount >= requiredAmount;
        }

        public float GetProgress()
        {
            return (float)currentAmount / requiredAmount;
        }
    }

    [System.Serializable]
    public class Recipe
    {
        public int id;
        public string name;
        public string description;
        public List<Ingredient> ingredients;
        public float cookingTime;
        public int rewardPoints;

        public bool IsComplete()
        {
            foreach (var ingredient in ingredients)
            {
                if (!ingredient.IsComplete())
                    return false;
            }
            return true;
        }

        public float GetOverallProgress()
        {
            if (ingredients.Count == 0) return 0f;

            float totalProgress = 0f;
            foreach (var ingredient in ingredients)
            {
                totalProgress += ingredient.GetProgress();
            }
            return totalProgress / ingredients.Count;
        }
    }

    [System.Serializable]
    public class RecipeDatabase
    {
        public List<Recipe> recipes;
    }
}