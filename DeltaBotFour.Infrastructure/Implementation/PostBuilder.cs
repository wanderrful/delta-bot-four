﻿using System.Collections.Generic;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PostBuilder : IPostBuilder
    {
        public string BuildDeltaLogPost(List<DeltaComment> deltaComments)
        {
            return "this is a deltalog post";
        }
    }
}