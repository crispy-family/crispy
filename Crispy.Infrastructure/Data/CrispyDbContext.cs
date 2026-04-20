using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Crispy.Infrastructure.Data
{
    public class CrispyDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public CrispyDbContext(DbContextOptions<CrispyDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<FavoriteRecipe> FavoriteRecipes { get; set; }
        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

        public DbSet<UserFollower> UserFollowers { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Цей рядок життєво необхідний для роботи системи авторизації!
            base.OnModelCreating(builder);

            // Вказуємо, що комбінація двох ID є первинним ключем
            builder.Entity<FavoriteRecipe>()
                .HasKey(fr => new { fr.UserId, fr.RecipeId });

            // Data Seeding для категорій
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Сніданки", Description = "Швидкі та смачні ідеї для початку дня" },
                new Category { Id = 2, Name = "Супи", Description = "Обідні супи та бульйони" },
                new Category { Id = 3, Name = "Основні страви", Description = "Гарніри та страви з м'яса або риби" },
                new Category { Id = 4, Name = "Салати", Description = "Легкі і поживні салати" },
                new Category { Id = 5, Name = "Десерти", Description = "Солодощі, торти, печиво" },
                new Category { Id = 6, Name = "Напої", Description = "Чай, кава, коктейлі" },
                new Category { Id = 7, Name = "Випічка", Description = "Хліб, пироги, здоба" }
            );

            builder.Entity<UserFollower>()
                .HasKey(uf => new { uf.FollowerId, uf.FollowedUserId });

            // 2. Налаштовуємо зв'язок для Читача
            builder.Entity<UserFollower>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict); // Обов'язково Restrict, щоб уникнути помилок SQL

            // 3. Налаштовуємо зв'язок для Автора
            builder.Entity<UserFollower>()
                .HasOne(uf => uf.FollowedUser)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MealPlan>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MealPlan>()
                .HasOne(m => m.Recipe)
                .WithMany()
                .HasForeignKey(m => m.RecipeId)
                .OnDelete(DeleteBehavior.Restrict); // або Cascade, залежить від вашої логіки
        }
    }
}
