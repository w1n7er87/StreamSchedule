﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StreamSchedule.Data;

#nullable disable

namespace StreamSchedule.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("StreamSchedule.Data.Models.Stream", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateOnly>("StreamDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("StreamStatus")
                        .HasColumnType("INTEGER");

                    b.Property<TimeOnly>("StreamTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("StreamTitle")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Streams");
                });

            modelBuilder.Entity("StreamSchedule.Data.Models.TextCommand", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Privileges")
                        .HasColumnType("INTEGER");

                    b.HasKey("Name");

                    b.ToTable("TextCommands");
                });

            modelBuilder.Entity("StreamSchedule.Data.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessagesOffline")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessagesOnline")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PreviousUsernames")
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .HasColumnType("TEXT");

                    b.Property<int>("privileges")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
