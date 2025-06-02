// email-templates.js
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

    // Load template editor
    function loadTemplateEditor(templateId = null) {
        currentTemplateId = templateId;

        $.ajax({
            url: templateId ? `/templates/edit/${templateId}` : "/templates/edit/",
            type: "GET",
            success: function (response) {
                $("#templateEditorContainer").html(response);
                $("#emailTemplateModal").modal("show");
                initializeTinyMCE();
                setupFormHandlers();
            },
            error: function () {
                alert("Failed to load template editor.");
            }
        });
    }

    // Initialize TinyMCE editor
    function initializeTinyMCE() {
        if (tinymce.get("templateBody")) {
            tinymce.remove("#templateBody");
        }

        tinymce.init({
            selector: "#templateBody",
            height: 250,
            menubar: false,
            plugins: "link image code table lists template fullscreen",
            toolbar: "undo redo | formatselect | bold italic | alignleft aligncenter alignright | bullist numlist | link image | fullscreen",
            toolbar_mode: "floating",
            content_style: "body { font-family: Arial, sans-serif; font-size: 14px; }",
            images_upload_handler: function (blobInfo, progress) {
                return new Promise((resolve, reject) => {
                    reject("Image uploads not supported in this demo.");
                });
            },
            setup: function (editor) {
                editor.on("change", function () {
                    editor.save();
                });
            },
            branding: false,
            promotion: false,
            referrer_policy: 'origin',
            valid_elements: '*[*]',
            extended_valid_elements: '*[*]',
            allow_conditional_comments: true,
            allow_html_in_named_anchor: true,
            allow_unsafe_link_target: true,
            convert_urls: false,
            relative_urls: false,
            remove_script_host: false,
            document_base_url: window.location.origin,
            api_key: '51rrjjws1sni0jozlqxcz8z0lfkhmqqmm0kg0ru7sqy1ghh3' // Replace with your TinyMCE API key
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
        const templateBody = tinymce.get("templateBody").getContent();

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
