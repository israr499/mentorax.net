// Chat initialization helper
function initChatForUser(userId, userName, userRole) {
    console.log("initChatForUser called:", { userId, userName, userRole });

    // Check if chatApp exists globally
    if (typeof window.chatApp === 'undefined') {
        console.log("chatApp not defined yet, waiting...");
        setTimeout(() => initChatForUser(userId, userName, userRole), 100);
        return;
    }

    if (!userId) {
        console.log("No userId provided");
        return false;
    }

    if (window.chatApp.currentUserId === userId && window.chatApp.connection?.state === "Connected") {
        console.log("Chat already initialized");
        return true;
    }

    window.chatApp.init(userId, userName, userRole);
    return true;
}

function loadUserChatRooms() {
    if (window.chatApp && window.chatApp.loadChatRooms && window.chatApp.currentUserId) {
        window.chatApp.loadChatRooms();
        return true;
    }
    return false;
}