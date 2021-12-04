$(function(){

    var header = $('#header');
    var scrollOffset = $(this).scrollTop();



    /* HeaderFixed */
    $(window).on("scroll load resize", function(){

        var introH = $('#intro').innerHeight();
        scrollOffset = $(this).scrollTop();

        if(scrollOffset >= introH){
            header.addClass('fixed');
        }
        else{
            header.removeClass('fixed');
        }

    });




    /* Smooth scroll */
    $("[data-scroll]").on("click", function(event){

        event.preventDefault();

        var $this = $(this)
        var elementId = $this.data('scroll');
        var elementOffset = $(elementId).offset().top;

        var futureOffset = elementOffset - header.innerHeight();

        $("html, body").animate({
            scrollTop: futureOffset
        }, 500)

        $("#nav_toggle").removeClass("active")
        $("#nav").removeClass("active")

    });





    /* Menu nav toggle */
    $("#nav_toggle").on("click", function(event){

        event.preventDefault();

        $(this).toggleClass("active")
        $("#nav").toggleClass("active")
		header.toggleClass("active")

    });
	
	
	/* Accordion */
    $("[data-collapse]").on("click", function(event){

        event.preventDefault();

        var $this = $(this);
        var elementId = $this.data('collapse');
        $(elementId).slideToggle();
        $this.toggleClass("active");

    });
	
	
	
	////////////////////////////////////////////////////////////////////////////////
//	modals  
////////////////////////////////////////////////////////////////////////////////
	
	$("[data-modal]").on("click", function(event){
		
		event.preventDefault();
		
		let $this = $(this);
		let modalId = $this.data('modal');
		
		$(modalId).addClass('show');
		
		isModalOpened = true;
		if(isHeaderFixed)
		{
			header.removeClass('fixed');
		}
		
		$("body").addClass('no-scroll');
		
//		setTimeout(function(){
//			$(modalId).find('.modal_dialog').css({
//				transform: "rotateX(0)"
//			});
//		}, 200);
		
		$('[data-slider="works_slider"]').slick('setPosition');
		
	});
	
	
	$("[data-close]").on("click", function(event){
		
		event.preventDefault();
		
		let $this = $(this);
		let modalParent = $this.parents('.modal');
		
		modalParent.removeClass('show');
		
		isModalOpened = false;
		if(isHeaderFixed)
		{
			header.addClass('fixed');
		}
		
		$("body").removeClass('no-scroll');
		
//		modalParent.find(".modal_dialog").css({
//			transform: "rotateX(90deg)"
//		});
//		
//		setTimeout(function(){
//			modalParent.removeClass('show');
//			$("body").removeClass('no-scroll');
//		}, 200);
		
	});
	
	
	$(".modal_dialog").on("click", function(event){
		
		event.stopPropagation();
		
	});
	
	$(".modal").on("click", function(event){
		
		event.preventDefault();
		
		$(this).removeClass('show');
		
		isModalOpened = false;
		if(isHeaderFixed)
		{
			header.addClass('fixed');
		}
		
		$("body").removeClass('no-scroll');
		
//		$(this).find(".modal_dialog").css({
//			transform: "rotateX(90deg)"
//		});
//		
//		setTimeout(function(){
//			$(this).removeClass('show');
//			$("body").removeClass('no-scroll');
//		}, 200);
		
	});
	

});