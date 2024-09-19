﻿using BussinessObjects.GroupFeature;
using BussinessObjects.MediaFeature;
using Connectify.BusinessObjects.Authen;
using Connectify.BusinessObjects.ChatFeature;
using Connectify.BusinessObjects.CommentFeature;
using Connectify.BusinessObjects.FriendFeature;
using Connectify.BusinessObjects.PostFeature;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connectify.Server.DataAccess
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext()
        {

        }
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //Post
            builder.Entity<Post>()
                .HasDiscriminator<string>("PostType")
                .HasValue<NormalPost>("Normal")
                .HasValue<GroupPost>("Group");
            builder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            //Friend request
            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Requester)
                .WithMany()
                .HasForeignKey(fr => fr.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            //FriendShip
            builder.Entity<FriendShip>()
                .HasOne(fs => fs.User1)
                .WithMany()
                .HasForeignKey(fr => fr.User1Id)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<FriendShip>()
                .HasOne(fs => fs.User2)
                .WithMany()
                .HasForeignKey(fr => fr.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CommentReaction>()
                .HasOne(ce => ce.User)
                .WithMany()
                .HasForeignKey(ce => ce.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            //GroupJoinRequest
            builder.Entity<GroupJoinRequest>()
                .HasOne(gr => gr.TargetUser)
                .WithMany()
                .HasForeignKey(gr => gr.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            //GroupInvitation
            builder.Entity<GroupInvitation>()
                .HasOne(gr => gr.Referrer)
                .WithMany()
                .HasForeignKey(gr => gr.ReferrerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<GroupInvitation>()
                .HasOne(gr => gr.TargetUser)
                .WithMany()
                .HasForeignKey(gr => gr.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
            //MediaFeature
            builder.Entity<Media>()
                .HasDiscriminator<string>("MediaType")
                .HasValue<PostMedia>("Post")
                .HasValue<MessageMedia>("Message");

            builder.Entity<PostMedia>()
                .HasOne(pm => pm.Post)                
                .WithMany(p => p.Media)               
                .HasForeignKey(pm => pm.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MessageReaction>()
                .HasOne(mr => mr.User)
                .WithMany()
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<FriendShip> FriendShips { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentReaction> CommentReactions { get; set; }
        public DbSet<AppGroup> AppGroups { get; set; }
        public DbSet<GroupSettings> GroupSettings { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupJoinRequest> GroupJoinRequests { get; set; }
        public DbSet<GroupInvitation> GroupInvitations { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<ChatRoomMember> ChatRoomMembers { get; set; }
    }
}
