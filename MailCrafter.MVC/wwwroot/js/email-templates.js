﻿// email-templates.js
$(document).ready(function () {
    // Initialize modal behavior
    let currentTemplateId = null;

    // Handle create template button click
    $("#createTemplateButton").click(function () {
        loadTemplateEditor();
    });

    // Handle edit template button click
    $(document).on("click", ".edit-template", function () {
        const templateId = $(this).data("id");
        loadTemplateEditor(templateId);
    });

    // Handle delete template button click
    $(document).on("click", ".delete-template", function () {
        currentTemplateId = $(this).data("id");
        $("#deleteConfirmationModal").modal("show");
    });

    // Handle delete confirmation
    $("#confirmDeleteButton").click(function () {
        if (!currentTemplateId) return;

        $.ajax({
            url: `/api/templates/${currentTemplateId}`,
            type: "DELETE",
            headers: {
                "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    $("#deleteConfirmationModal").modal("hide");
                    window.location.reload();
                } else {
                    alert("Failed to delete template. Please try again.");
                }
            },
            error: function () {
                alert("An error occurred while deleting the template.");
            }
        });
    });

    // Initialize CKEditor
    function initializeCKEditor() {
        const editorContainer = document.getElementById('templateBody');
        if (!editorContainer) return;

        // Initialize CKEditor
        ClassicEditor
            .create(editorContainer, {
                toolbar: {
                    items: [
                        'heading',
                        '|',
                        'bold',
                        'italic',
                        'link',
                        'bulletedList',
                        'numberedList',
                        '|',
                        'outdent',
                        'indent',
                        '|',
                        'blockQuote',
                        'insertTable',
                        'undo',
                        'redo'
                    ]
                },
                table: {
                    contentToolbar: [
                        'tableColumn',
                        'tableRow',
                        'mergeTableCells'
                    ]
                },
                placeholder: 'Write your email template here...',
                removePlugins: ['Title', 'DocumentOutline']
            })
            .then(editor => {
                // Store editor instance for later use
                window.ckEditor = editor;

                // Set editor height
                editor.editing.view.change(writer => {
                    writer.setStyle('height', '300px', editor.editing.view.document.getRoot());
                });

                // If there's existing content, set it
                const existingContent = editorContainer.getAttribute('data-content');
                if (existingContent) {
                    editor.setData(existingContent);
                }
            })
            .catch(error => {
                console.error(error);
            });
    }

    // Load template editor
    function loadTemplateEditor(templateId = null) {
        currentTemplateId = templateId;

        $.ajax({
            url: templateId ? `/templates/edit/${templateId}` : "/templates/edit/",
            type: "GET",
            success: function (response) {
                $("#templateEditorContainer").html(response);
                $("#emailTemplateModal").modal("show");
                
                // Initialize CKEditor after the modal is shown
                $("#emailTemplateModal").on('shown.bs.modal', function () {
                    initializeCKEditor();
                });
                
                setupFormHandlers();
            },
            error: function () {
                alert("Failed to load template editor.");
            }
        });
    }

    // Set up form handlers for the template editor
    function setupFormHandlers() {
        // Handle placeholder addition
        $("#addPlaceholderBtn").click(function () {
            const newPlaceholder = `
                <div class="input-group mb-2 placeholder-item">
                    <input type="text" class="form-control placeholder-value" placeholder="Placeholder name">
                    <button class="btn btn-outline-danger remove-placeholder" type="button">×</button>
                </div>
            `;
            $("#placeholdersContainer").append(newPlaceholder);
        });

        // Handle placeholder removal
        $(document).on("click", ".remove-placeholder", function () {
            if ($("#placeholdersContainer .placeholder-item").length > 1) {
                $(this).closest(".placeholder-item").remove();
            } else {
                $(this).closest(".placeholder-item").find("input").val("");
            }
        });

        // Handle file upload button
        $("#uploadButton").click(function () {
            $("#fileUpload").click();
        });

        // Handle file selection
        $("#fileUpload").change(function () {
            const files = this.files;
            if (!files || files.length === 0) return;

            uploadFiles(files);
        });

        // Handle attachment removal
        $(document).on("click", ".remove-attachment", function () {
            $(this).closest(".attachment-item").remove();
        });

        // Handle form submission
        $("#saveTemplateBtn").click(function () {
            saveTemplate();
        });
    }

    // Upload files
    function uploadFiles(files) {
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            const formData = new FormData();
            formData.append("file", file);

            $.ajax({
                url: "/api/templates/upload",
                type: "POST",
                data: formData,
                processData: false,
                contentType: false,
                success: function (response) {
                    addAttachmentToList(response);
                },
                error: function () {
                    alert("Failed to upload file: " + file.name);
                }
            });
        }
    }

    // Add attachment to the list
    function addAttachmentToList(fileInfo) {
        const attachmentHtml = `
            <div class="card mb-2 attachment-item">
                <div class="card-body p-2 d-flex justify-content-between align-items-center">
                    <div>
                        <input type="hidden" class="attachment-url" value="${fileInfo.fileUrl}" />
                        <input type="hidden" class="attachment-type" value="${fileInfo.fileType}" />
                        <span class="attachment-name">${fileInfo.fileName}</span>
                    </div>
                    <button type="button" class="btn btn-sm btn-outline-danger remove-attachment">×</button>
                </div>
            </div>
        `;
        $("#attachmentsContainer").append(attachmentHtml);
    }

    // Save template
    function saveTemplate() {
        // Show loading indicator
        $("#saveSpinner").removeClass("d-none");
        $("#saveTemplateBtn").attr("disabled", true);

        // Get form data
        const templateId = $("#templateId").val();
        const templateName = $("#templateName").val().trim();
        const templateSubject = $("#templateSubject").val().trim();
        const templateBody = window.ckEditor ? window.ckEditor.getData() : '';

        // Validate form
        if (!templateName) {
            alert("Please enter a template name.");
            $("#saveSpinner").addClass("d-none");
            $("#saveTemplateBtn").removeAttr("disabled");
            return;
        }

        if (!templateSubject) {
            alert("Please enter an email subject.");
            $("#saveSpinner").addClass("d-none");
            $("#saveTemplateBtn").removeAttr("disabled");
            return;
        }

        // Get placeholders
        const placeholders = [];
        $(".placeholder-value").each(function () {
            const value = $(this).val().trim();
            if (value) {
                placeholders.push(value);
            }
        });

        // Get attachments
        const attachments = [];
        $(".attachment-item").each(function () {
            const fileName = $(this).find(".attachment-name").text();
            const fileUrl = $(this).find(".attachment-url").val();
            const fileType = $(this).find(".attachment-type").val();

            attachments.push({
                fileName: fileName,
                fileType: fileType,
                fileUrl: fileUrl
            });
        });

        // Create template object
        const template = {
            id: templateId || "",
            name: templateName,
            subject: templateSubject,
            body: templateBody,
            placeholders: placeholders,
            attachments: attachments,
            inlineImages: []
        };

        // Save template
        $.ajax({
            url: "/api/templates",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(template),
            success: function (response) {
                if (response.success) {
                    $("#emailTemplateModal").modal("hide");
                    window.location.reload();
                } else {
                    alert("Failed to save template. Please try again.");
                }
            },
            error: function (xhr) {
                const message = xhr.responseJSON?.message || "An error occurred while saving the template.";
                alert(message);
            },
            complete: function () {
                $("#saveSpinner").addClass("d-none");
                $("#saveTemplateBtn").removeAttr("disabled");
            }
        });
    }
});

// Modal open logic for AI chat
$(document).ready(function() {
    $("#openAiChatModalBtn").on("click", function() {
        $("#aiChatModal").modal("show");
    });
});

// Gemini AI Chat Box logic
(function() {
    const API_KEY = "AIzaSyCg44cvQxaUrOdPZ49KqR2Li7ffIpATdnQ";
    const API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + API_KEY;
    const chatMessages = document.getElementById("chatMessages");
    const chatInput = document.getElementById("chatInput");
    const sendBtn = document.getElementById("sendChatBtn");
    let isWaiting = false;

    function appendMessage(text, sender) {
        // Replace \n with <br> for AI messages
        let html = sender === "ai" ? text.replace(/\n/g, "<br>") : text;
        const msgDiv = document.createElement("div");
        msgDiv.className = sender === "user" ? "text-end mb-2" : "text-start mb-2";
        msgDiv.innerHTML = `<span class='badge ${sender === "user" ? "text-white" : "bg-light text-dark"}' style='font-size:1em; background-color : #FF6B00;'>${html}</span>`;
        chatMessages.appendChild(msgDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function setWaiting(state) {
        isWaiting = state;
        sendBtn.disabled = state;
        chatInput.disabled = state;
        if(state) appendMessage("Answering...", "ai-wait");
    }

    function removeWaiting() {
        // Remove last waiting message
        const nodes = chatMessages.querySelectorAll(".text-start, .text-end");
        if (nodes.length > 0 && nodes[nodes.length - 1].textContent === "Answering...") {
            chatMessages.removeChild(nodes[nodes.length-1]);
        }
    }

    async function sendMessage() {
        const ViewText = chatInput.value.trim();
        const text = 'generate no code email content with placeholders form like {{placeholder}} with ideal ' + chatInput.value.trim();
        if (!text || isWaiting) return;
        appendMessage(ViewText, "user");
        chatInput.value = "";
        setWaiting(true);
        try {
            const res = await fetch(API_URL, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    contents: [{ parts: [{ text }] }]
                })
            });
            const data = await res.json();
            removeWaiting();
            if (data && data.candidates && data.candidates[0] && data.candidates[0].content && data.candidates[0].content.parts) {
                const aiText = data.candidates[0].content.parts.map(p => p.text).join("\n");
                appendMessage(aiText, "ai");
            } else {
                appendMessage("AI không trả lời được. Vui lòng thử lại.", "ai");
            }
        } catch (e) {
            removeWaiting();
            appendMessage("Lỗi khi gọi AI: " + e.message, "ai");
        }
        setWaiting(false);
    }

    if (chatInput && sendBtn) {
        sendBtn.addEventListener("click", sendMessage);
        chatInput.addEventListener("keydown", function(e) {
            if (e.key === "Enter") sendMessage();
        });
    }
})();
