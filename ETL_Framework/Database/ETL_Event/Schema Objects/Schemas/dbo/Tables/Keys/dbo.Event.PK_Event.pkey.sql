﻿ALTER TABLE [dbo].[Event]
    ADD CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED ([EventTypeID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);

