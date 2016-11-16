CREATE CONTRACT [ETLController]
    AUTHORIZATION [dbo]
    ([ETLController_InfoMessage] SENT BY TARGET, [ETLController_Request] SENT BY INITIATOR, [ETLController_Receipt] SENT BY TARGET, [ETLController_Test] SENT BY ANY, ETLController_Cancel SENT BY INITIATOR);

