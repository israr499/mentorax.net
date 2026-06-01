// ============================================================
//  MENTORAX CHAT — FULLY FIXED VERSION
//
//  FIXES APPLIED:
//    1. Role display: "Chat with Student" shown to Tutor,
//                     "Chat with Tutor" shown to Student
//    2. startChatWithTutor: properly defined and exposed to window
//    3. All API calls use port 7232 (ChatService), not 7027
//    4. No duplicate messages (hub broadcasts to sender too,
//       so NO optimistic render — onReceive handles everything)
//    5. Close button works (closeChat exposed on window)
//    6. Messages show real text (safe fallbacks for undefined)
//    7. var used throughout (IE/older browser compatibility)
//    8. No double-init guard (prevents second SignalR connection)
// ============================================================

(function () {
    'use strict';

    // ── Module-level state ──────────────────────────────────
    var connection = null;
    var _userId = null;
    var _userName = null;
    var _userRole = null;
    var _roomId = null;
    var _receiverId = null;
    var _view = 'rooms';
    var _messageIds = {};       // plain object as Set for older browsers
    var _initialized = false;
    var _typingTimer = null;

    // ── DOM helpers ─────────────────────────────────────────
    function $id(id) { return document.getElementById(id); }
    function chatWin() { return $id('chatWindow'); }
    function chatMsgs() { return $id('chatMessages'); }
    function chatTitle() { return $id('chatWindowTitle'); }
    function chatInput() { return $id('chatInput'); }
    function typingEl() { return $id('typingIndicator'); }
    function badge() { return $id('unreadChatBadge'); }

    // ── Init (called from _Layout) ───────────────────────────
    function initChat(userId, userName, role) {
        if (_initialized && _userId === userId) {
            console.log('[Chat] Already initialized for', userId);
            return;
        }
        _initialized = true;
        _userId = userId;
        _userName = userName;
        _userRole = role;
        console.log('[Chat] initChat userId=' + userId + ' role=' + role);

        connection = new signalR.HubConnectionBuilder()
            .withUrl('https://mentorax-chat-gabwajbratc0d0f4.southeastasia-01.azurewebsites.net/chathub?userId=' + userId)
            .withAutomaticReconnect([0, 1000, 3000, 5000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.on('ReceiveMessage', onReceive);
        connection.on('UserTyping', onTyping);

        connection.start()
            .then(function () {
                console.log('[Chat] SignalR connected');
                refreshUnreadBadge();
            })
            .catch(function (err) {
                console.error('[Chat] Connection failed:', err);
            });
    }

    // ── Receive handler ──────────────────────────────────────
    function onReceive(data) {
        console.log('[Chat] ReceiveMessage', data);

        var msgId = data.messageId || data.MessageId || data.MessageID;
        if (!msgId) return;
        if (_messageIds[msgId]) return;  // deduplicate
        _messageIds[msgId] = true;

        var roomId = data.roomId || data.RoomId || data.RoomID;
        if (_roomId && _roomId === roomId) {
            appendBubble(data);
        }
        refreshUnreadBadge();
    }

    function onTyping(data) {
        if (_roomId !== (data.roomId || data.RoomId)) return;
        var el = typingEl();
        if (!el) return;
        if (data.isTyping) {
            el.textContent = (data.userName || 'Someone') + ' is typing\u2026';
            el.style.display = 'block';
        } else {
            el.style.display = 'none';
        }
    }

    // ── Toggle / open chat panel ─────────────────────────────
    function toggleChatPanel() {
        var win = chatWin();
        if (!win) return;
        if (win.style.display === 'flex') {
            win.style.display = 'none';
        } else {
            win.style.display = 'flex';
            showRoomList();
        }
    }

    function closeChat() {
        var win = chatWin();
        if (win) win.style.display = 'none';
        _roomId = null;
        _view = 'rooms';
    }

    // ── Room list view ───────────────────────────────────────
    function showRoomList() {
        _view = 'rooms';
        setTitle('<i class="fas fa-comments me-2"></i>Messages');
        var container = chatMsgs();
        if (!container) return;
        container.innerHTML = '<div class="loading-state"><i class="fas fa-spinner fa-spin"></i><p>Loading\u2026</p></div>';

        fetch('https://mentorax-chat-gabwajbratc0d0f4.southeastasia-01.azurewebsites.net/api/Chat/GetUserRooms/' + _userId)
            .then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                return res.json();
            })
            .then(function (rooms) {
                renderRoomList(rooms, container);
            })
            .catch(function (err) {
                console.error('[Chat] loadRooms error', err);
                container.innerHTML = '<div class="empty-state"><i class="fas fa-exclamation-circle"></i><p>Could not load conversations.</p></div>';
            });
    }

    function renderRoomList(rooms, container) {
        if (!rooms || rooms.length === 0) {
            container.innerHTML = '<div class="empty-state"><i class="fas fa-comment-slash"></i><p>No conversations yet.<br>Click <strong>Message Tutor</strong> to start one.</p></div>';
            return;
        }

        var html = '';
        for (var i = 0; i < rooms.length; i++) {
            var room = rooms[i];

            // Support both camelCase (ASP.NET default) and PascalCase
            var roomId = room.roomID || room.RoomID;
            var studentId = room.studentID || room.StudentID;
            var tutorId = room.tutorID || room.TutorID;
            var unread = room.unreadCount != null ? room.unreadCount : (room.UnreadCount || 0);

            // ── FIX: CORRECT ROLE DISPLAY ──────────────────────────────
            // If the current user's ID matches StudentID → they are the Student
            //   → show "Chat with Tutor"
            // If the current user's ID matches TutorID → they are the Tutor
            //   → show "Chat with Student"
            var isCurrentUserStudent = (studentId === _userId);
            var otherLabel = isCurrentUserStudent ? 'Tutor' : 'Student';
            var otherId = isCurrentUserStudent ? tutorId : studentId;

            var badgeHtml = '';
            if (unread > 0) {
                badgeHtml = '<span class="badge bg-danger rounded-pill ms-auto">' + unread + '</span>';
            }

            html += '<div class="chat-room-item d-flex align-items-center gap-2"' +
                ' data-room="' + roomId + '"' +
                ' data-other-id="' + otherId + '"' +
                ' data-other-name="' + otherLabel + '">' +
                '<div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0"' +
                ' style="width:38px;height:38px;background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;font-weight:700">' +
                otherLabel[0] +
                '</div>' +
                '<div class="flex-grow-1 overflow-hidden">' +
                '<div class="fw-semibold text-truncate" style="font-size:13px">Chat with ' + otherLabel + '</div>' +
                '<div class="text-muted" style="font-size:11px">Tap to open</div>' +
                '</div>' +
                badgeHtml +
                '</div>';
        }

        container.innerHTML = html;

        var items = container.querySelectorAll('.chat-room-item');
        for (var j = 0; j < items.length; j++) {
            (function (el) {
                el.addEventListener('click', function () {
                    openRoom(el.dataset.room, el.dataset.otherName, el.dataset.otherId);
                });
            })(items[j]);
        }
    }

    // ── Open a specific room ─────────────────────────────────
    function openRoom(roomId, otherName, otherId) {
        if (!roomId || roomId === 'undefined' || roomId === '') {
            console.error('[Chat] Invalid roomId:', roomId);
            return;
        }
        console.log('[Chat] openRoom', roomId, otherName, otherId);

        _roomId = roomId;
        _receiverId = otherId;
        _view = 'messages';
        _messageIds = {};  // clear dedup tracker for new room

        var win = chatWin();
        if (win) win.style.display = 'flex';

        setTitle(
            '<span style="cursor:pointer;opacity:.8;margin-right:8px" onclick="window.chatGoBack()">' +
            '<i class="fas fa-arrow-left"></i>' +
            '</span>' +
            '<i class="fas fa-circle text-success me-1" style="font-size:8px;vertical-align:middle"></i>' +
            (otherName || 'Chat')
        );

        var container = chatMsgs();
        if (container) {
            container.innerHTML = '<div class="loading-state"><i class="fas fa-spinner fa-spin"></i><p>Loading\u2026</p></div>';
        }

        if (connection && connection.state === 'Connected') {
            connection.invoke('JoinRoom', roomId).catch(function (e) {
                console.error('[Chat] JoinRoom failed', e);
            });
        }

        loadMessages(roomId);

        var inp = chatInput();
        if (inp) inp.focus();
    }

    function chatGoBack() {
        _roomId = null;
        showRoomList();
    }

    // ── Load message history ─────────────────────────────────
    function loadMessages(roomId) {
        var container = chatMsgs();
        if (!container) return;

        fetch('https://mentorax-chat-gabwajbratc0d0f4.southeastasia-01.azurewebsites.net/api/Chat/GetMessages/' + roomId)
            .then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                return res.json();
            })
            .then(function (msgs) {
                container.innerHTML = '';

                if (!msgs || msgs.length === 0) {
                    container.innerHTML = '<div class="empty-state"><i class="fas fa-comment-dots"></i><p>No messages yet.<br>Say hello!</p></div>';
                    return;
                }

                for (var i = 0; i < msgs.length; i++) {
                    var msg = msgs[i];

                    // Support both casings
                    var msgId = msg.messageID || msg.MessageID || '';
                    var senderId = msg.senderID || msg.SenderID || '';
                    var senderName = msg.senderName || msg.SenderName || 'User';
                    var text = msg.message || msg.Message || '';
                    var time = msg.sentAt || msg.SentAt || new Date().toISOString();

                    if (msgId) _messageIds[msgId] = true;

                    var isMine = (senderId === _userId);
                    appendBubble({
                        messageId: msgId,
                        senderUserId: senderId,
                        senderName: isMine ? 'You' : senderName,
                        message: text,
                        sentAt: time
                    });
                }

                container.scrollTop = container.scrollHeight;
            })
            .catch(function (err) {
                console.error('[Chat] loadMessages error', err);
                if (container) {
                    container.innerHTML = '<div class="empty-state"><i class="fas fa-exclamation-circle"></i><p>Could not load messages.</p></div>';
                }
            });
    }

    // ── Render a single message bubble ───────────────────────
    function appendBubble(data) {
        var container = chatMsgs();
        if (!container) return;

        // Remove placeholder states
        var empty = container.querySelector('.empty-state, .loading-state');
        if (empty) empty.parentNode.removeChild(empty);

        var senderId = data.senderUserId || data.SenderUserId || data.senderID || '';
        var isMine = (senderId === _userId);
        var cls = isMine ? 'message-sent' : 'message-received';
        var name = isMine ? 'You' : (data.senderName || 'Other');
        var text = escapeHtml(data.message || data.Message || '');
        var timeStr = formatTime(data.sentAt || data.SentAt);

        var div = document.createElement('div');
        div.className = cls;
        div.innerHTML =
            '<div class="message-bubble">' +
            '<span class="fw-semibold">' + name + '</span>' +
            '<p>' + text + '</p>' +
            '<small>' + timeStr + '</small>' +
            '</div>';

        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    }

    // ── Send message ─────────────────────────────────────────
    function sendMessage() {
        var inp = chatInput();
        var text = inp ? inp.value.trim() : '';

        if (!text) return;

        if (!_roomId) {
            alert('No conversation open. Open a chat room first.');
            return;
        }

        if (!connection || connection.state !== 'Connected') {
            alert('Chat not connected. Please refresh the page.');
            return;
        }

        inp.value = '';
        inp.disabled = true;

        // DO NOT do optimistic render here.
        // The SignalR hub broadcasts ReceiveMessage to ALL group members
        // including the sender, so onReceive() will render the bubble.
        // Rendering here AND in onReceive = duplicate messages.
        connection.invoke('SendMessage', _roomId, _userId, text)
            .catch(function (err) {
                console.error('[Chat] send error', err);
                inp.value = text;  // restore on failure
                alert('Failed to send: ' + err.message);
            })
            .finally(function () {
                inp.disabled = false;
                if (inp) inp.focus();
            });
    }

    // ── Typing indicator ─────────────────────────────────────
    function onChatInputKeydown(e) {
        if (e && e.key === 'Enter') { sendMessage(); return; }
        if (!connection || !_roomId) return;

        connection.invoke('Typing', _roomId, _userName, true).catch(function () { });
        clearTimeout(_typingTimer);
        _typingTimer = setTimeout(function () {
            if (connection && _roomId) {
                connection.invoke('Typing', _roomId, _userName, false).catch(function () { });
            }
        }, 1500);
    }

    // ── Start chat from TutorDetails page ────────────────────
    // FIX: This function was either missing or silently failing.
    // Now properly defined and exposed on window.
    function startChatWithTutor(tutorId, tutorName) {
        console.log('[Chat] startChatWithTutor tutorId=' + tutorId + ' tutorName=' + tutorName);

        if (!_userId) {
            alert('Please login first.');
            return;
        }
        if (!tutorId || tutorId === 'undefined' || tutorId === '') {
            alert('Tutor ID not found. Cannot open chat.');
            return;
        }

        var win = chatWin();
        if (win) win.style.display = 'flex';

        setTitle('<i class="fas fa-spinner fa-spin me-2"></i>Opening chat\u2026');

        var chatContainer = chatMsgs();
        if (chatContainer) {
            chatContainer.innerHTML = '<div class="loading-state"><i class="fas fa-spinner fa-spin"></i><p>Starting chat\u2026</p></div>';
        }

        // Use ChatService directly on port 7232
        var url = 'https://mentorax-chat-gabwajbratc0d0f4.southeastasia-01.azurewebsites.net/api/Chat/GetOrCreateRoom?studentId=' + _userId + '&tutorId=' + tutorId;

        fetch(url)
            .then(function (res) {
                if (!res.ok) {
                    return res.text().then(function (txt) {
                        throw new Error('API ' + res.status + ': ' + txt);
                    });
                }
                return res.json();
            })
            .then(function (room) {
                var roomId = room.roomID || room.RoomID;
                console.log('[Chat] room created/found:', roomId);
                if (!roomId) throw new Error('Room ID missing from response');
                openRoom(roomId, tutorName || 'Tutor', tutorId);
            })
            .catch(function (err) {
                console.error('[Chat] startChatWithTutor error', err);
                setTitle('<i class="fas fa-comments me-2"></i>Messages');
                var cm = chatMsgs();
                if (cm) {
                    cm.innerHTML = '<div class="empty-state">' +
                        '<i class="fas fa-exclamation-circle text-danger"></i>' +
                        '<p>Could not open chat.<br><small>' + escapeHtml(err.message) + '</small></p>' +
                        '</div>';
                }
            });
    }

    // ── Unread badge ─────────────────────────────────────────
    function refreshUnreadBadge() {
        if (!_userId) return;

        fetch('https://mentorax-chat-gabwajbratc0d0f4.southeastasia-01.azurewebsites.net/api/Chat/GetUnreadCount/' + _userId)
            .then(function (res) {
                if (!res.ok) return;
                return res.json();
            })
            .then(function (data) {
                if (!data) return;
                var count = data.unreadCount || 0;
                var el = badge();
                if (!el) return;
                if (count > 0) {
                    el.textContent = count > 99 ? '99+' : String(count);
                    el.style.display = 'block';
                } else {
                    el.style.display = 'none';
                }
            })
            .catch(function () { /* silently ignore */ });
    }

    // ── Helpers ──────────────────────────────────────────────
    function setTitle(html) {
        var el = chatTitle();
        if (el) el.innerHTML = html;
    }

    function formatTime(iso) {
        try {
            return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        } catch (e) {
            return '';
        }
    }

    function escapeHtml(text) {
        var d = document.createElement('div');
        d.textContent = String(text);
        return d.innerHTML;
    }

    function loadChatRooms() { showRoomList(); }

    // ── Expose to window ─────────────────────────────────────
    window.initChat = initChat;
    window.toggleChatPanel = toggleChatPanel;
    window.closeChat = closeChat;
    window.sendMessage = sendMessage;
    window.openRoom = openRoom;
    window.loadChatRooms = loadChatRooms;
    window.loadMessages = loadMessages;
    window.startChatWithTutor = startChatWithTutor;
    window.onChatInputKeydown = onChatInputKeydown;
    window.chatGoBack = chatGoBack;

    console.log('[Chat] chat.js loaded (fixed version)');
})();
